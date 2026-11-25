using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Data;
using SistemaEventos.Models;
using SistemaEventos.ViewModels;

namespace SistemaEventos.Controllers
{
    /// <summary>
    /// Cadastro de contas, login, logout e dados do usuário (ASP.NET Core Identity).
    /// </summary>
    public class ContaController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ContaController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /Conta/Registrar
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Registrar()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegistroViewModel());
        }

        // POST: /Conta/Registrar
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistroViewModel modelo)
        {
            if (ModelState.IsValid && await _userManager.FindByEmailAsync(modelo.Email.Trim()) != null)
            {
                ModelState.AddModelError(nameof(modelo.Email), "Este e-mail já está cadastrado. Faça login ou use outro e-mail.");
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var email = modelo.Email.Trim();
            var usuario = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NomeCompleto = modelo.NomeCompleto.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(modelo.Telefone) ? null : modelo.Telefone.Trim(),
                DataCadastro = DateTime.Now
            };

            var resultado = await _userManager.CreateAsync(usuario, modelo.Senha);
            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors.Select(e => e.Description).Distinct())
                {
                    ModelState.AddModelError(string.Empty, erro);
                }
                return View(modelo);
            }

            // Toda conta criada pelo site recebe o perfil de usuário comum
            await _userManager.AddToRoleAsync(usuario, Perfis.Usuario);
            await _signInManager.SignInAsync(usuario, isPersistent: false);

            TempData["Sucesso"] = $"Conta criada com sucesso! Bem-vindo(a), {PrimeiroNome(usuario.NomeCompleto)}. Escolha um evento para se inscrever.";
            return RedirectToAction("Index", "Eventos");
        }

        // GET: /Conta/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Conta/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var resultado = await _signInManager.PasswordSignInAsync(
                modelo.Email.Trim(), modelo.Senha, modelo.LembrarMe, lockoutOnFailure: true);

            if (resultado.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Conta bloqueada temporariamente após várias tentativas sem sucesso. Tente novamente em alguns minutos.");
                return View(modelo);
            }

            if (!resultado.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(modelo);
            }

            var usuario = await _userManager.FindByEmailAsync(modelo.Email.Trim());
            TempData["Sucesso"] = $"Olá, {PrimeiroNome(usuario?.NomeCompleto)}! Você entrou no sistema.";

            if (!string.IsNullOrEmpty(modelo.ReturnUrl) && Url.IsLocalUrl(modelo.ReturnUrl))
            {
                return LocalRedirect(modelo.ReturnUrl);
            }

            if (usuario != null && await _userManager.IsInRoleAsync(usuario, Perfis.Admin))
            {
                return RedirectToAction("Index", "Painel", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Eventos");
        }

        // POST: /Conta/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Info"] = "Você saiu da sua conta.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Conta/AcessoNegado
        [AllowAnonymous]
        public IActionResult AcessoNegado(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // GET: /Conta/MinhaConta
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MinhaConta()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return Challenge();
            }

            return View(await MontarMinhaContaAsync(usuario));
        }

        // POST: /Conta/MinhaConta
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MinhaConta(MinhaContaViewModel modelo)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                var dados = await MontarMinhaContaAsync(usuario);
                dados.NomeCompleto = modelo.NomeCompleto;
                dados.Telefone = modelo.Telefone;
                return View(dados);
            }

            usuario.NomeCompleto = modelo.NomeCompleto.Trim();
            usuario.PhoneNumber = string.IsNullOrWhiteSpace(modelo.Telefone) ? null : modelo.Telefone.Trim();

            var resultado = await _userManager.UpdateAsync(usuario);
            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, erro.Description);
                }
                return View(await MontarMinhaContaAsync(usuario));
            }

            // Atualiza o cookie para refletir o novo nome no menu
            await _signInManager.RefreshSignInAsync(usuario);
            TempData["Sucesso"] = "Seus dados foram atualizados.";
            return RedirectToAction(nameof(MinhaConta));
        }

        // GET: /Conta/AlterarSenha
        [Authorize]
        [HttpGet]
        public IActionResult AlterarSenha()
        {
            return View(new AlterarSenhaViewModel());
        }

        // POST: /Conta/AlterarSenha
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return Challenge();
            }

            var resultado = await _userManager.ChangePasswordAsync(usuario, modelo.SenhaAtual, modelo.NovaSenha);
            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, erro.Description);
                }
                return View(modelo);
            }

            await _signInManager.RefreshSignInAsync(usuario);
            TempData["Sucesso"] = "Senha alterada com sucesso.";
            return RedirectToAction(nameof(MinhaConta));
        }

        private async Task<MinhaContaViewModel> MontarMinhaContaAsync(ApplicationUser usuario)
        {
            var perfis = await _userManager.GetRolesAsync(usuario);

            return new MinhaContaViewModel
            {
                NomeCompleto = usuario.NomeCompleto,
                Telefone = usuario.PhoneNumber,
                Email = usuario.Email,
                Perfil = perfis.Contains(Perfis.Admin) ? "Administrador" : "Usuário",
                DataCadastro = usuario.DataCadastro,
                TotalInscricoes = await _context.Inscricoes.CountAsync(i => i.UsuarioId == usuario.Id),
                InscricoesAtivas = await _context.Inscricoes.CountAsync(i =>
                    i.UsuarioId == usuario.Id &&
                    (i.Status == StatusInscricao.Pendente || i.Status == StatusInscricao.Confirmada))
            };
        }

        private static string PrimeiroNome(string? nomeCompleto)
        {
            if (string.IsNullOrWhiteSpace(nomeCompleto))
            {
                return "visitante";
            }

            return nomeCompleto.Trim().Split(' ')[0];
        }
    }
}

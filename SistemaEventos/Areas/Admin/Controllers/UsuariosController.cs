using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Data;
using SistemaEventos.Models;
using SistemaEventos.ViewModels;

namespace SistemaEventos.Areas.Admin.Controllers
{
    /// <summary>
    /// Consulta das contas cadastradas e do histórico de inscrições de cada uma.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = Perfis.Admin)]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Usuarios?busca=...
        public async Task<IActionResult> Index(string? busca)
        {
            var consulta = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(u => u.NomeCompleto.Contains(termo) || u.Email!.Contains(termo));
            }

            var usuarios = await consulta
                .OrderBy(u => u.NomeCompleto)
                .Select(u => new UsuarioItemViewModel
                {
                    Id = u.Id,
                    NomeCompleto = u.NomeCompleto,
                    Email = u.Email ?? string.Empty,
                    Telefone = u.PhoneNumber,
                    DataCadastro = u.DataCadastro,
                    TotalInscricoes = u.Inscricoes.Count(),
                    InscricoesAtivas = u.Inscricoes.Count(i =>
                        i.Status == StatusInscricao.Pendente || i.Status == StatusInscricao.Confirmada)
                })
                .ToListAsync();

            await PreencherPerfisAsync(usuarios);

            ViewData["Busca"] = busca;
            return View(usuarios);
        }

        // GET: /Admin/Usuarios/Details/{id}
        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var usuario = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UsuarioItemViewModel
                {
                    Id = u.Id,
                    NomeCompleto = u.NomeCompleto,
                    Email = u.Email ?? string.Empty,
                    Telefone = u.PhoneNumber,
                    DataCadastro = u.DataCadastro,
                    TotalInscricoes = u.Inscricoes.Count(),
                    InscricoesAtivas = u.Inscricoes.Count(i =>
                        i.Status == StatusInscricao.Pendente || i.Status == StatusInscricao.Confirmada)
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return NotFound();
            }

            await PreencherPerfisAsync(new List<UsuarioItemViewModel> { usuario });

            var inscricoes = await _context.Inscricoes
                .AsNoTracking()
                .Where(i => i.UsuarioId == id)
                .OrderByDescending(i => i.DataInscricao)
                .Select(InscricaoItemViewModel.Projecao)
                .ToListAsync();

            return View(new AdminUsuarioDetalhesViewModel { Usuario = usuario, Inscricoes = inscricoes });
        }

        /// <summary>Busca os perfis (AspNetUserRoles + AspNetRoles) de cada usuário.</summary>
        private async Task PreencherPerfisAsync(List<UsuarioItemViewModel> usuarios)
        {
            var ids = usuarios.Select(u => u.Id).ToList();

            var perfis = await (from ur in _context.UserRoles
                                join r in _context.Roles on ur.RoleId equals r.Id
                                where ids.Contains(ur.UserId)
                                select new { ur.UserId, r.Name })
                               .ToListAsync();

            foreach (var usuario in usuarios)
            {
                usuario.Perfis = string.Join(", ", perfis
                    .Where(p => p.UserId == usuario.Id)
                    .Select(p => p.Name));
            }
        }
    }
}

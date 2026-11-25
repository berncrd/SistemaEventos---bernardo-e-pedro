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
    /// Operação principal do usuário: inscrever-se em eventos e acompanhar as próprias inscrições.
    /// Acesso exclusivo para contas com o perfil "Usuario".
    /// </summary>
    [Authorize(Roles = Perfis.Usuario)]
    public class InscricoesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InscricoesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string UsuarioLogadoId => _userManager.GetUserId(User)!;

        // GET: /Inscricoes  (Minhas inscrições)
        public async Task<IActionResult> Index(StatusInscricao? status)
        {
            var usuarioId = UsuarioLogadoId;

            // Cada usuário vê SOMENTE as próprias inscrições
            var consulta = _context.Inscricoes
                .AsNoTracking()
                .Where(i => i.UsuarioId == usuarioId);

            if (status.HasValue)
            {
                consulta = consulta.Where(i => i.Status == status.Value);
            }

            var inscricoes = await consulta
                .OrderByDescending(i => i.DataInscricao)
                .Select(InscricaoItemViewModel.Projecao)
                .ToListAsync();

            var contagens = await _context.Inscricoes
                .Where(i => i.UsuarioId == usuarioId)
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Total = g.Count() })
                .ToListAsync();

            var modelo = new MinhasInscricoesViewModel
            {
                StatusFiltro = status,
                Inscricoes = inscricoes,
                Contagens = contagens.ToDictionary(c => c.Status, c => c.Total)
            };

            return View(modelo);
        }

        // GET: /Inscricoes/Detalhes/5
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuarioId = UsuarioLogadoId;

            // O filtro por UsuarioId impede que um usuário veja a inscrição de outro
            var inscricao = await _context.Inscricoes
                .AsNoTracking()
                .Where(i => i.Id == id.Value && i.UsuarioId == usuarioId)
                .Select(InscricaoItemViewModel.Projecao)
                .FirstOrDefaultAsync();

            if (inscricao == null)
            {
                return NotFound();
            }

            return View(inscricao);
        }

        // GET: /Inscricoes/Nova/5  (tela de confirmação)
        [HttpGet]
        public async Task<IActionResult> Nova(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await BuscarEventoAsync(id.Value);
            if (evento == null)
            {
                return NotFound();
            }

            var erro = await VerificarRegrasAsync(evento, UsuarioLogadoId);
            if (erro != null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction("Details", "Eventos", new { id = evento.Id });
            }

            return View(new NovaInscricaoViewModel { EventoId = evento.Id, Evento = evento });
        }

        // POST: /Inscricoes/Nova
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nova(NovaInscricaoViewModel modelo)
        {
            var evento = await BuscarEventoAsync(modelo.EventoId);
            if (evento == null)
            {
                return NotFound();
            }

            modelo.Evento = evento;
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var usuarioId = UsuarioLogadoId;

            // As regras são verificadas novamente no servidor antes de gravar
            var erro = await VerificarRegrasAsync(evento, usuarioId);
            if (erro != null)
            {
                ModelState.AddModelError(string.Empty, erro);
                return View(modelo);
            }

            // Eventos gratuitos são confirmados na hora; pagos aguardam confirmação do administrador
            var statusInicial = evento.Preco == 0 ? StatusInscricao.Confirmada : StatusInscricao.Pendente;

            // Se existir uma inscrição cancelada, ela é reaproveitada (índice único EventoId + UsuarioId)
            var inscricao = await _context.Inscricoes
                .FirstOrDefaultAsync(i => i.EventoId == evento.Id && i.UsuarioId == usuarioId);

            if (inscricao == null)
            {
                inscricao = new Inscricao { EventoId = evento.Id, UsuarioId = usuarioId };
                _context.Inscricoes.Add(inscricao);
            }
            else
            {
                inscricao.DataAtualizacao = DateTime.Now;
            }

            inscricao.Status = statusInicial;
            inscricao.DataInscricao = DateTime.Now;
            inscricao.Valor = evento.Preco; // preço preservado no momento da inscrição
            inscricao.Observacao = string.IsNullOrWhiteSpace(modelo.Observacao) ? null : modelo.Observacao.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível concluir a inscrição. Verifique se você já está inscrito e tente novamente.");
                return View(modelo);
            }

            TempData["Sucesso"] = statusInicial == StatusInscricao.Confirmada
                ? $"Inscrição confirmada em \"{evento.Titulo}\"! Nos vemos lá."
                : $"Inscrição realizada em \"{evento.Titulo}\". Ela ficará pendente até a confirmação do pagamento pela organização.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Inscricoes/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var usuarioId = UsuarioLogadoId;

            var inscricao = await _context.Inscricoes
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(i => i.Id == id && i.UsuarioId == usuarioId);

            if (inscricao == null)
            {
                return NotFound();
            }

            if (inscricao.Status == StatusInscricao.Cancelada)
            {
                TempData["Aviso"] = "Esta inscrição já estava cancelada.";
            }
            else if (inscricao.Status == StatusInscricao.Concluida || inscricao.Evento!.DataInicio <= DateTime.Now)
            {
                // Regra: cancelamento permitido somente antes do início do evento
                TempData["Erro"] = "Não é possível cancelar: o evento já começou ou a participação já foi concluída.";
            }
            else
            {
                inscricao.Status = StatusInscricao.Cancelada;
                inscricao.DataAtualizacao = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = $"Inscrição em \"{inscricao.Evento!.Titulo}\" cancelada. Sua vaga foi liberada.";
            }

            return RedirectToAction(nameof(Index));
        }

        private Task<EventoResumoViewModel?> BuscarEventoAsync(int eventoId)
        {
            return _context.Eventos
                .AsNoTracking()
                .Where(e => e.Id == eventoId)
                .Select(EventoResumoViewModel.Projecao)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Regras de negócio verificadas ANTES de gravar a inscrição.
        /// Retorna a mensagem de erro ou null quando a inscrição é permitida.
        /// </summary>
        private async Task<string?> VerificarRegrasAsync(EventoResumoViewModel evento, string usuarioId)
        {
            // REGRA 1 — Bloqueio após o encerramento: só aceita inscrição em evento publicado e que ainda não começou
            if (!evento.Ativo)
            {
                return "Este evento não está aberto para inscrições.";
            }

            if (evento.DataInicio <= DateTime.Now)
            {
                return "As inscrições para este evento estão encerradas.";
            }

            // REGRA 2 — Inscrição sem duplicidade: o usuário não pode ter duas inscrições ativas no mesmo evento
            var jaInscrito = await _context.Inscricoes.AnyAsync(i =>
                i.EventoId == evento.Id &&
                i.UsuarioId == usuarioId &&
                i.Status != StatusInscricao.Cancelada);

            if (jaInscrito)
            {
                return "Você já está inscrito(a) neste evento. Acompanhe em \"Minhas inscrições\".";
            }

            // REGRA 3 — Vagas disponíveis: inscrições ativas não podem ultrapassar o limite de vagas
            var inscritosAtivos = await _context.Inscricoes.CountAsync(i =>
                i.EventoId == evento.Id && i.Status != StatusInscricao.Cancelada);

            if (inscritosAtivos >= evento.Vagas)
            {
                return "Não há mais vagas disponíveis para este evento.";
            }

            return null;
        }
    }
}

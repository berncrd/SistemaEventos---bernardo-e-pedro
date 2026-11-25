using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Data;
using SistemaEventos.Models;
using SistemaEventos.ViewModels;

namespace SistemaEventos.Controllers
{
    /// <summary>
    /// Catálogo público de eventos: qualquer visitante pode consultar.
    /// Para se inscrever é preciso entrar com uma conta de usuário.
    /// </summary>
    public class EventosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Eventos?busca=...&categoriaId=...&somenteComVagas=true
        public async Task<IActionResult> Index(string? busca, int? categoriaId, bool somenteComVagas = false)
        {
            var agora = DateTime.Now;

            // Somente eventos publicados e que ainda não começaram
            var consulta = _context.Eventos
                .AsNoTracking()
                .Where(e => e.Ativo && e.DataInicio > agora);

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(e =>
                    e.Titulo.Contains(termo) ||
                    e.Local.Contains(termo) ||
                    e.Descricao.Contains(termo));
            }

            if (categoriaId.HasValue)
            {
                consulta = consulta.Where(e => e.CategoriaId == categoriaId.Value);
            }

            if (somenteComVagas)
            {
                consulta = consulta.Where(e =>
                    e.Inscricoes.Count(i => i.Status != StatusInscricao.Cancelada) < e.Vagas);
            }

            var eventos = await consulta
                .OrderBy(e => e.DataInicio)
                .Select(EventoResumoViewModel.Projecao)
                .ToListAsync();

            await MarcarInscricoesDoUsuarioAsync(eventos);

            var categorias = await _context.Categorias.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();

            var modelo = new CatalogoViewModel
            {
                Busca = busca,
                CategoriaId = categoriaId,
                SomenteComVagas = somenteComVagas,
                Categorias = new SelectList(categorias, "Id", "Nome", categoriaId),
                Eventos = eventos
            };

            return View(modelo);
        }

        // GET: /Eventos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos
                .AsNoTracking()
                .Where(e => e.Id == id.Value)
                .Select(EventoResumoViewModel.Projecao)
                .FirstOrDefaultAsync();

            // Eventos em rascunho só são visíveis para o administrador
            if (evento == null || (!evento.Ativo && !User.IsInRole(Perfis.Admin)))
            {
                return NotFound();
            }

            var modelo = new EventoDetalhesViewModel { Evento = evento };

            if (User.IsInRole(Perfis.Usuario))
            {
                var usuarioId = _userManager.GetUserId(User);
                var minhaInscricao = await _context.Inscricoes
                    .AsNoTracking()
                    .Where(i => i.EventoId == evento.Id && i.UsuarioId == usuarioId)
                    .Select(i => new { i.Id, i.Status })
                    .FirstOrDefaultAsync();

                if (minhaInscricao != null)
                {
                    modelo.MinhaInscricaoId = minhaInscricao.Id;
                    modelo.MinhaInscricaoStatus = minhaInscricao.Status;
                    evento.UsuarioInscrito = minhaInscricao.Status != StatusInscricao.Cancelada;
                }
            }

            return View(modelo);
        }

        /// <summary>Marca os eventos em que o usuário logado já possui inscrição ativa.</summary>
        private async Task MarcarInscricoesDoUsuarioAsync(List<EventoResumoViewModel> eventos)
        {
            if (!User.IsInRole(Perfis.Usuario) || eventos.Count == 0)
            {
                return;
            }

            var usuarioId = _userManager.GetUserId(User);
            var idsEventos = eventos.Select(e => e.Id).ToList();

            var inscritos = await _context.Inscricoes
                .AsNoTracking()
                .Where(i => i.UsuarioId == usuarioId
                            && idsEventos.Contains(i.EventoId)
                            && i.Status != StatusInscricao.Cancelada)
                .Select(i => i.EventoId)
                .ToListAsync();

            foreach (var evento in eventos)
            {
                evento.UsuarioInscrito = inscritos.Contains(evento.Id);
            }
        }
    }
}

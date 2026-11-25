using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Data;
using SistemaEventos.Extensions;
using SistemaEventos.Models;
using SistemaEventos.ViewModels;

namespace SistemaEventos.Areas.Admin.Controllers
{
    /// <summary>
    /// Acompanhamento das inscrições de todos os usuários: busca, detalhes e atualização de status.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = Perfis.Admin)]
    public class InscricoesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InscricoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Inscricoes?busca=...&status=...&eventoId=...
        public async Task<IActionResult> Index(string? busca, StatusInscricao? status, int? eventoId)
        {
            var consulta = _context.Inscricoes.AsNoTracking();

            if (status.HasValue)
            {
                consulta = consulta.Where(i => i.Status == status.Value);
            }

            if (eventoId.HasValue)
            {
                consulta = consulta.Where(i => i.EventoId == eventoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().TrimStart('#');

                if (int.TryParse(termo, out var numero))
                {
                    consulta = consulta.Where(i => i.Id == numero);
                }
                else
                {
                    consulta = consulta.Where(i =>
                        i.Usuario!.NomeCompleto.Contains(termo) ||
                        i.Usuario!.Email!.Contains(termo) ||
                        i.Evento!.Titulo.Contains(termo));
                }
            }

            var inscricoes = await consulta
                .OrderByDescending(i => i.DataInscricao)
                .Select(InscricaoItemViewModel.Projecao)
                .ToListAsync();

            var eventos = await _context.Eventos
                .AsNoTracking()
                .OrderByDescending(e => e.DataInicio)
                .Select(e => new { e.Id, e.Titulo })
                .ToListAsync();

            var modelo = new AdminInscricoesViewModel
            {
                Busca = busca,
                Status = status,
                EventoId = eventoId,
                Eventos = new SelectList(eventos, "Id", "Titulo", eventoId),
                Inscricoes = inscricoes
            };

            return View(modelo);
        }

        // GET: /Admin/Inscricoes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inscricao = await _context.Inscricoes
                .AsNoTracking()
                .Where(i => i.Id == id.Value)
                .Select(InscricaoItemViewModel.Projecao)
                .FirstOrDefaultAsync();

            if (inscricao == null)
            {
                return NotFound();
            }

            var vagas = await _context.Eventos
                .Where(e => e.Id == inscricao.EventoId)
                .Select(e => e.Vagas)
                .FirstAsync();

            var ativos = await _context.Inscricoes.CountAsync(i =>
                i.EventoId == inscricao.EventoId && i.Status != StatusInscricao.Cancelada);

            return View(new AdminInscricaoDetalhesViewModel
            {
                Inscricao = inscricao,
                EventoVagas = vagas,
                EventoInscritosAtivos = ativos,
                NovoStatus = inscricao.Status
            });
        }

        // POST: /Admin/Inscricoes/AtualizarStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarStatus(int id, StatusInscricao novoStatus)
        {
            var inscricao = await _context.Inscricoes
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inscricao == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid || !Enum.IsDefined(typeof(StatusInscricao), novoStatus))
            {
                TempData["Erro"] = "Status inválido.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (inscricao.Status == novoStatus)
            {
                TempData["Info"] = $"A inscrição #{id} já está com o status {novoStatus.Nome()}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var evento = inscricao.Evento!;
            string? erro = null;

            // Regra: reativar uma inscrição cancelada exige vaga livre e evento ainda não iniciado
            if (inscricao.Status == StatusInscricao.Cancelada)
            {
                if (evento.DataInicio <= DateTime.Now)
                {
                    erro = "Não é possível reativar inscrições de um evento que já começou.";
                }
                else
                {
                    var ativos = await _context.Inscricoes.CountAsync(i =>
                        i.EventoId == evento.Id && i.Status != StatusInscricao.Cancelada);

                    if (ativos >= evento.Vagas)
                    {
                        erro = "Não há vagas disponíveis para reativar esta inscrição.";
                    }
                }
            }

            // Regra: a participação só pode ser marcada como concluída depois que o evento começou
            if (erro == null && novoStatus == StatusInscricao.Concluida && evento.DataInicio > DateTime.Now)
            {
                erro = "A inscrição só pode ser marcada como concluída após o início do evento.";
            }

            if (erro != null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction(nameof(Details), new { id });
            }

            var statusAnterior = inscricao.Status;
            inscricao.Status = novoStatus;
            inscricao.DataAtualizacao = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Inscrição #{id} atualizada: {statusAnterior.Nome()} → {novoStatus.Nome()}.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

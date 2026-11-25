using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Data;
using SistemaEventos.Models;
using SistemaEventos.ViewModels;

namespace SistemaEventos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Perfis.Admin)]
    public class PainelController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PainelController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Painel
        public async Task<IActionResult> Index()
        {
            var agora = DateTime.Now;

            var modelo = new PainelViewModel
            {
                EventosAbertos = await _context.Eventos.CountAsync(e => e.Ativo && e.DataInicio > agora),
                TotalEventos = await _context.Eventos.CountAsync(),
                InscricoesAtivas = await _context.Inscricoes.CountAsync(i =>
                    i.Status == StatusInscricao.Pendente || i.Status == StatusInscricao.Confirmada),
                InscricoesPendentes = await _context.Inscricoes.CountAsync(i => i.Status == StatusInscricao.Pendente),
                TotalUsuarios = await _context.Users.CountAsync(),
                TotalCategorias = await _context.Categorias.CountAsync(),
                ReceitaConfirmada = await _context.Inscricoes
                    .Where(i => i.Status == StatusInscricao.Confirmada || i.Status == StatusInscricao.Concluida)
                    .SumAsync(i => i.Valor),
                ProximosEventos = await _context.Eventos
                    .AsNoTracking()
                    .Where(e => e.DataInicio > agora)
                    .OrderBy(e => e.DataInicio)
                    .Take(5)
                    .Select(EventoResumoViewModel.Projecao)
                    .ToListAsync(),
                UltimasInscricoes = await _context.Inscricoes
                    .AsNoTracking()
                    .OrderByDescending(i => i.DataInscricao)
                    .Take(6)
                    .Select(InscricaoItemViewModel.Projecao)
                    .ToListAsync()
            };

            return View(modelo);
        }
    }
}

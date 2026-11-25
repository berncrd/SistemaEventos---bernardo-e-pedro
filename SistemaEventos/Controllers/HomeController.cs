using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Data;
using SistemaEventos.Models;
using SistemaEventos.ViewModels;

namespace SistemaEventos.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var agora = DateTime.Now;

            var modelo = new HomeViewModel
            {
                ProximosEventos = await _context.Eventos
                    .AsNoTracking()
                    .Where(e => e.Ativo && e.DataInicio > agora)
                    .OrderBy(e => e.DataInicio)
                    .Take(3)
                    .Select(EventoResumoViewModel.Projecao)
                    .ToListAsync(),
                TotalEventosAbertos = await _context.Eventos.CountAsync(e => e.Ativo && e.DataInicio > agora),
                TotalCategorias = await _context.Categorias.CountAsync(),
                TotalParticipacoes = await _context.Inscricoes.CountAsync(i => i.Status != StatusInscricao.Cancelada)
            };

            return View(modelo);
        }

        public IActionResult Sobre()
        {
            return View();
        }

        /// <summary>Página amigável para erros HTTP (404, 403...).</summary>
        [Route("/erro/{codigo:int}")]
        public IActionResult CodigoStatus(int codigo)
        {
            ViewData["Codigo"] = codigo;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Data;
using SistemaEventos.Models;
using SistemaEventos.Services;
using SistemaEventos.ViewModels;

namespace SistemaEventos.Areas.Admin.Controllers
{
    /// <summary>
    /// CRUD completo de eventos com upload de imagem (somente administrador).
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = Perfis.Admin)]
    public class EventosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImagemService _imagens;

        public EventosController(ApplicationDbContext context, IImagemService imagens)
        {
            _context = context;
            _imagens = imagens;
        }

        // GET: /Admin/Eventos?busca=...&categoriaId=...&situacao=...
        public async Task<IActionResult> Index(string? busca, int? categoriaId, string? situacao)
        {
            var agora = DateTime.Now;
            var consulta = _context.Eventos.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(e => e.Titulo.Contains(termo) || e.Local.Contains(termo));
            }

            if (categoriaId.HasValue)
            {
                consulta = consulta.Where(e => e.CategoriaId == categoriaId.Value);
            }

            consulta = situacao switch
            {
                "abertos" => consulta.Where(e => e.Ativo && e.DataInicio > agora),
                "rascunhos" => consulta.Where(e => !e.Ativo),
                "encerrados" => consulta.Where(e => e.DataInicio <= agora),
                "lotados" => consulta.Where(e => e.Inscricoes.Count(i => i.Status != StatusInscricao.Cancelada) >= e.Vagas),
                _ => consulta
            };

            var eventos = await consulta
                .OrderBy(e => e.DataInicio <= agora) // próximos primeiro, depois os já iniciados
                .ThenBy(e => e.DataInicio)
                .Select(EventoResumoViewModel.Projecao)
                .ToListAsync();

            var modelo = new AdminEventosViewModel
            {
                Busca = busca,
                CategoriaId = categoriaId,
                Situacao = situacao,
                Categorias = new SelectList(await _context.Categorias.AsNoTracking().OrderBy(c => c.Nome).ToListAsync(), "Id", "Nome", categoriaId),
                Eventos = eventos
            };

            return View(modelo);
        }

        // GET: /Admin/Eventos/Details/5
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

            if (evento == null)
            {
                return NotFound();
            }

            var inscricoes = await _context.Inscricoes
                .AsNoTracking()
                .Where(i => i.EventoId == evento.Id)
                .OrderBy(i => i.DataInscricao)
                .Select(InscricaoItemViewModel.Projecao)
                .ToListAsync();

            return View(new AdminEventoDetalhesViewModel { Evento = evento, Inscricoes = inscricoes });
        }

        // GET: /Admin/Eventos/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var inicio = DateTime.Today.AddDays(7).AddHours(19);
            var evento = new Evento
            {
                DataInicio = inicio,
                DataFim = inicio.AddHours(3),
                Vagas = 50,
                Preco = 0,
                Ativo = true
            };

            await CarregarCategoriasAsync(null);
            return View(evento);
        }

        // POST: /Admin/Eventos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Titulo,Descricao,DataInicio,DataFim,Local,Vagas,Preco,Ativo,CategoriaId")] Evento evento,
            IFormFile? imagemArquivo)
        {
            if (ModelState.IsValid)
            {
                // Regra: um novo evento precisa começar em uma data futura
                if (evento.DataInicio <= DateTime.Now)
                {
                    ModelState.AddModelError(nameof(Evento.DataInicio), "A data de início deve ser futura.");
                }

                if (!await _context.Categorias.AnyAsync(c => c.Id == evento.CategoriaId))
                {
                    ModelState.AddModelError(nameof(Evento.CategoriaId), "Selecione uma categoria válida.");
                }
            }

            var erroImagem = _imagens.Validar(imagemArquivo);
            if (erroImagem != null)
            {
                ModelState.AddModelError("imagemArquivo", erroImagem);
            }

            if (!ModelState.IsValid)
            {
                await CarregarCategoriasAsync(evento.CategoriaId);
                return View(evento);
            }

            if (imagemArquivo != null && imagemArquivo.Length > 0)
            {
                evento.ImagemUrl = await _imagens.SalvarAsync(imagemArquivo);
            }

            evento.Titulo = evento.Titulo.Trim();
            evento.Local = evento.Local.Trim();

            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Evento \"{evento.Titulo}\" cadastrado com sucesso.";
            return RedirectToAction(nameof(Details), new { id = evento.Id });
        }

        // GET: /Admin/Eventos/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos.FindAsync(id.Value);
            if (evento == null)
            {
                return NotFound();
            }

            await CarregarCategoriasAsync(evento.CategoriaId);
            return View(evento);
        }

        // POST: /Admin/Eventos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Titulo,Descricao,DataInicio,DataFim,Local,Vagas,Preco,Ativo,CategoriaId")] Evento evento,
            IFormFile? imagemArquivo,
            bool removerImagem = false)
        {
            if (id != evento.Id)
            {
                return NotFound();
            }

            var existente = await _context.Eventos.FirstOrDefaultAsync(e => e.Id == id);
            if (existente == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Regra: o número de vagas não pode ficar menor que as inscrições ativas
                var inscritosAtivos = await _context.Inscricoes.CountAsync(i =>
                    i.EventoId == id && i.Status != StatusInscricao.Cancelada);

                if (evento.Vagas < inscritosAtivos)
                {
                    ModelState.AddModelError(nameof(Evento.Vagas),
                        $"Este evento já possui {inscritosAtivos} inscrição(ões) ativa(s). O número de vagas não pode ser menor que isso.");
                }

                if (!await _context.Categorias.AnyAsync(c => c.Id == evento.CategoriaId))
                {
                    ModelState.AddModelError(nameof(Evento.CategoriaId), "Selecione uma categoria válida.");
                }
            }

            var erroImagem = _imagens.Validar(imagemArquivo);
            if (erroImagem != null)
            {
                ModelState.AddModelError("imagemArquivo", erroImagem);
            }

            if (!ModelState.IsValid)
            {
                evento.ImagemUrl = existente.ImagemUrl;
                await CarregarCategoriasAsync(evento.CategoriaId);
                return View(evento);
            }

            existente.Titulo = evento.Titulo.Trim();
            existente.Descricao = evento.Descricao;
            existente.DataInicio = evento.DataInicio;
            existente.DataFim = evento.DataFim;
            existente.Local = evento.Local.Trim();
            existente.Vagas = evento.Vagas;
            existente.Preco = evento.Preco;
            existente.Ativo = evento.Ativo;
            existente.CategoriaId = evento.CategoriaId;

            if (imagemArquivo != null && imagemArquivo.Length > 0)
            {
                _imagens.Excluir(existente.ImagemUrl);
                existente.ImagemUrl = await _imagens.SalvarAsync(imagemArquivo);
            }
            else if (removerImagem)
            {
                _imagens.Excluir(existente.ImagemUrl);
                existente.ImagemUrl = null;
            }

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Evento \"{existente.Titulo}\" atualizado com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Admin/Eventos/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
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

            if (evento == null)
            {
                return NotFound();
            }

            return View(evento);
        }

        // POST: /Admin/Eventos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
            {
                return NotFound();
            }

            // Regra: evento com inscrições ativas não pode ser excluído (preserva o histórico)
            var inscritosAtivos = await _context.Inscricoes.CountAsync(i =>
                i.EventoId == id && i.Status != StatusInscricao.Cancelada);

            if (inscritosAtivos > 0)
            {
                TempData["Erro"] = $"O evento possui {inscritosAtivos} inscrição(ões) ativa(s) e não pode ser excluído. Desmarque \"Publicado\" para encerrar as inscrições.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var imagem = evento.ImagemUrl;
            _context.Eventos.Remove(evento); // inscrições canceladas são removidas em cascata
            await _context.SaveChangesAsync();
            _imagens.Excluir(imagem);

            TempData["Sucesso"] = $"Evento \"{evento.Titulo}\" excluído.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CarregarCategoriasAsync(int? categoriaSelecionada)
        {
            var categorias = await _context.Categorias.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();
            ViewData["CategoriaId"] = new SelectList(categorias, "Id", "Nome", categoriaSelecionada);
        }
    }
}

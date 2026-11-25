using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Data;
using SistemaEventos.Models;
using SistemaEventos.ViewModels;

namespace SistemaEventos.Areas.Admin.Controllers
{
    /// <summary>
    /// CRUD completo de categorias (somente administrador).
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = Perfis.Admin)]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Categorias
        public async Task<IActionResult> Index(string? busca)
        {
            var consulta = _context.Categorias.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim();
                consulta = consulta.Where(c =>
                    c.Nome.Contains(termo) || (c.Descricao != null && c.Descricao.Contains(termo)));
            }

            var categorias = await consulta
                .OrderBy(c => c.Nome)
                .Select(c => new CategoriaListaViewModel
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Descricao = c.Descricao,
                    TotalEventos = c.Eventos.Count()
                })
                .ToListAsync();

            ViewData["Busca"] = busca;
            return View(categorias);
        }

        // GET: /Admin/Categorias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias
                .AsNoTracking()
                .Include(c => c.Eventos.OrderBy(e => e.DataInicio))
                .FirstOrDefaultAsync(c => c.Id == id.Value);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // GET: /Admin/Categorias/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Categoria());
        }

        // POST: /Admin/Categorias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Descricao")] Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                categoria.Nome = categoria.Nome.Trim();

                // Regra: não permitir categorias com o mesmo nome
                if (await NomeEmUsoAsync(categoria.Nome, idIgnorado: null))
                {
                    ModelState.AddModelError(nameof(Categoria.Nome), "Já existe uma categoria com este nome.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Categoria \"{categoria.Nome}\" cadastrada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Categorias/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias.FindAsync(id.Value);
            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // POST: /Admin/Categorias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Descricao")] Categoria categoria)
        {
            if (id != categoria.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                categoria.Nome = categoria.Nome.Trim();
                if (await NomeEmUsoAsync(categoria.Nome, idIgnorado: id))
                {
                    ModelState.AddModelError(nameof(Categoria.Nome), "Já existe outra categoria com este nome.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(categoria);
            }

            try
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Categorias.AnyAsync(c => c.Id == id))
                {
                    return NotFound();
                }
                throw;
            }

            TempData["Sucesso"] = $"Categoria \"{categoria.Nome}\" atualizada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Categorias/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Id == id.Value)
                .Select(c => new CategoriaListaViewModel
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Descricao = c.Descricao,
                    TotalEventos = c.Eventos.Count()
                })
                .FirstOrDefaultAsync();

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // POST: /Admin/Categorias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }

            // Regra: categoria com eventos vinculados não pode ser excluída
            var totalEventos = await _context.Eventos.CountAsync(e => e.CategoriaId == id);
            if (totalEventos > 0)
            {
                TempData["Erro"] = $"A categoria \"{categoria.Nome}\" possui {totalEventos} evento(s) vinculado(s) e não pode ser excluída. Mova ou exclua os eventos primeiro.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = $"Categoria \"{categoria.Nome}\" excluída.";
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> NomeEmUsoAsync(string nome, int? idIgnorado)
        {
            return _context.Categorias.AnyAsync(c => c.Nome == nome && (idIgnorado == null || c.Id != idIgnorado));
        }
    }
}

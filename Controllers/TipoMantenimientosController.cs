using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class TipoMantenimientosController : Controller
    {
        private readonly AppDbContext _context;

        public TipoMantenimientosController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.TiposMantenimiento.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(t => t.Nombre.Contains(buscar));

            int total = await query.CountAsync();

            var tipos = await query
                .OrderBy(t => t.Nombre)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(tipos);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var tipo = await _context.TiposMantenimiento
                .Include(t => t.MantenimientosCarros).ThenInclude(m => m.Carro)
                .FirstOrDefaultAsync(t => t.IdTipoMante == id);

            if (tipo == null) return NotFound();
            return View(tipo);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoMantenimiento tipo)
        {
            ModelState.Remove("MantenimientosCarros");

            if (ModelState.IsValid)
            {
                if (await _context.TiposMantenimiento.AnyAsync(t => t.Nombre == tipo.Nombre))
                {
                    ModelState.AddModelError("Nombre", "Ya existe un tipo de mantenimiento con ese nombre.");
                    return View(tipo);
                }

                _context.Add(tipo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Tipo \"{tipo.Nombre}\" registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(tipo);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var tipo = await _context.TiposMantenimiento.FirstOrDefaultAsync(t => t.IdTipoMante == id);
            if (tipo == null) return NotFound();
            return View(tipo);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TipoMantenimiento tipo)
        {
            if (id != tipo.IdTipoMante) return NotFound();

            ModelState.Remove("MantenimientosCarros");

            if (ModelState.IsValid)
            {
                if (await _context.TiposMantenimiento.AnyAsync(t => t.Nombre == tipo.Nombre && t.IdTipoMante != id))
                {
                    ModelState.AddModelError("Nombre", "Ya existe otro tipo con ese nombre.");
                    return View(tipo);
                }

                try
                {
                    _context.Update(tipo);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Tipo \"{tipo.Nombre}\" actualizado.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.TiposMantenimiento.AnyAsync(t => t.IdTipoMante == id)) return NotFound();
                    throw;
                }
            }
            return View(tipo);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var tipo = await _context.TiposMantenimiento
                .Include(t => t.MantenimientosCarros)
                .FirstOrDefaultAsync(t => t.IdTipoMante == id);
            if (tipo == null) return NotFound();
            return View(tipo);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipo = await _context.TiposMantenimiento.FirstOrDefaultAsync(t => t.IdTipoMante == id);
            if (tipo == null) return NotFound();

            try
            {
                _context.TiposMantenimiento.Remove(tipo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tipo de mantenimiento eliminado.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: tiene mantenimientos asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
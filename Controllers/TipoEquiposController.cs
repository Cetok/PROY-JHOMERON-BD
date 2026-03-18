using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class TipoEquiposController : Controller
    {
        private readonly AppDbContext    _context;
        private readonly AuditoriaService _auditoriaService;

        public TipoEquiposController(AppDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? orden = "az", int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.TiposEquipo.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(t => t.tipo != null && t.tipo.Contains(buscar));

            int total = await query.CountAsync();

            var tipos = await (orden == "za"
                ? query.OrderByDescending(t => t.tipo)
                : query.OrderBy(t => t.tipo))
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Orden        = orden;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(tipos);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var tipo = await _context.TiposEquipo
                .FirstOrDefaultAsync(t => t.idTipoEquipo == id);

            if (tipo == null) return NotFound();

            // Cuántos equipos usan este tipo
            ViewBag.TotalEquipos = await _context.Equipos
                .CountAsync(e => e.idTipoEquipo == id);

            return View(tipo);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoEquipo tipoEquipo)
        {
            if (ModelState.IsValid)
            {
                bool existe = await _context.TiposEquipo
                    .AnyAsync(t => t.tipo == tipoEquipo.tipo);

                if (existe)
                {
                    ModelState.AddModelError("tipo", "Ya existe un tipo de equipo con ese nombre.");
                    return View(tipoEquipo);
                }

                _context.Add(tipoEquipo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Tipo '{tipoEquipo.tipo}' registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(tipoEquipo);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var tipo = await _context.TiposEquipo
                .FirstOrDefaultAsync(t => t.idTipoEquipo == id);

            if (tipo == null) return NotFound();
            return View(tipo);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TipoEquipo tipoEquipo)
        {
            if (id != tipoEquipo.idTipoEquipo) return NotFound();

            if (ModelState.IsValid)
            {
                bool existe = await _context.TiposEquipo
                    .AnyAsync(t => t.tipo == tipoEquipo.tipo && t.idTipoEquipo != id);

                if (existe)
                {
                    ModelState.AddModelError("tipo", "Ya existe otro tipo con ese nombre.");
                    return View(tipoEquipo);
                }

                try
                {
                    _context.Update(tipoEquipo);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Tipo '{tipoEquipo.tipo}' actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.TiposEquipo.AnyAsync(t => t.idTipoEquipo == id))
                        return NotFound();
                    throw;
                }
            }
            return View(tipoEquipo);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var tipo = await _context.TiposEquipo
                .FirstOrDefaultAsync(t => t.idTipoEquipo == id);

            if (tipo == null) return NotFound();

            ViewBag.TotalEquipos = await _context.Equipos
                .CountAsync(e => e.idTipoEquipo == id);

            return View(tipo);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipo = await _context.TiposEquipo
                .FirstOrDefaultAsync(t => t.idTipoEquipo == id);

            if (tipo == null) return NotFound();

            try
            {
                _context.TiposEquipo.Remove(tipo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Tipo '{tipo.tipo}' eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar porque hay equipos asociados a este tipo.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class MotivosController : Controller
    {
        private readonly AppDbContext _context;

        public MotivosController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Motivos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(m => m.TipoMotivo.Contains(buscar));

            int total = await query.CountAsync();

            var motivos = await query
                .OrderBy(m => m.TipoMotivo)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(motivos);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var motivo = await _context.Motivos
                .FirstOrDefaultAsync(m => m.IdMotivo == id);

            if (motivo == null) return NotFound();

            ViewBag.TotalHistoriales = await _context.Historiales
                .CountAsync(h => h.IdMotivo == id);

            return View(motivo);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Motivo motivo)
        {
            ModelState.Remove("Historiales");

            if (ModelState.IsValid)
            {
                bool existe = await _context.Motivos
                    .AnyAsync(m => m.TipoMotivo == motivo.TipoMotivo);

                if (existe)
                {
                    ModelState.AddModelError("TipoMotivo", "Ya existe un motivo con ese nombre.");
                    return View(motivo);
                }

                _context.Add(motivo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Motivo '{motivo.TipoMotivo}' registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(motivo);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var motivo = await _context.Motivos
                .FirstOrDefaultAsync(m => m.IdMotivo == id);

            if (motivo == null) return NotFound();
            return View(motivo);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Motivo motivo)
        {
            if (id != motivo.IdMotivo) return NotFound();

            ModelState.Remove("Historiales");

            if (ModelState.IsValid)
            {
                bool existe = await _context.Motivos
                    .AnyAsync(m => m.TipoMotivo == motivo.TipoMotivo && m.IdMotivo != id);

                if (existe)
                {
                    ModelState.AddModelError("TipoMotivo", "Ya existe otro motivo con ese nombre.");
                    return View(motivo);
                }

                try
                {
                    _context.Update(motivo);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Motivo '{motivo.TipoMotivo}' actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Motivos.AnyAsync(m => m.IdMotivo == id))
                        return NotFound();
                    throw;
                }
            }
            return View(motivo);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var motivo = await _context.Motivos
                .FirstOrDefaultAsync(m => m.IdMotivo == id);

            if (motivo == null) return NotFound();

            ViewBag.TotalHistoriales = await _context.Historiales
                .CountAsync(h => h.IdMotivo == id);

            return View(motivo);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var motivo = await _context.Motivos
                .FirstOrDefaultAsync(m => m.IdMotivo == id);

            if (motivo == null) return NotFound();

            try
            {
                _context.Motivos.Remove(motivo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Motivo '{motivo.TipoMotivo}' eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este motivo porque tiene historiales asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
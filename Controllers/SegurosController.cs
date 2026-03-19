using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class SegurosController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public SegurosController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Seguros
                .Include(s => s.CarroSeguros)
                .Include(s => s.EmpleadoSeguros)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(s => s.TipoSeguro.Contains(buscar));

            int total = await query.CountAsync();

            var seguros = await query
                .OrderBy(s => s.TipoSeguro)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(seguros);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var seguro = await _context.Seguros
                .Include(s => s.CarroSeguros).ThenInclude(cs => cs.Carro)
                .Include(s => s.EmpleadoSeguros).ThenInclude(es => es.Empleado)
                .FirstOrDefaultAsync(s => s.IdSeguro == id);

            if (seguro == null) return NotFound();
            return View(seguro);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Seguro seguro)
        {
            ModelState.Remove("EmpleadoSeguros");
            ModelState.Remove("CarroSeguros");

            if (ModelState.IsValid)
            {
                if (await _context.Seguros.AnyAsync(s => s.TipoSeguro == seguro.TipoSeguro))
                {
                    ModelState.AddModelError("TipoSeguro", "Ya existe un seguro con ese nombre.");
                    return View(seguro);
                }

                _context.Add(seguro);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Seguro \"{seguro.TipoSeguro}\" registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = seguro.IdSeguro });
            }
            return View(seguro);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var seguro = await _context.Seguros.FirstOrDefaultAsync(s => s.IdSeguro == id);
            if (seguro == null) return NotFound();
            return View(seguro);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Seguro seguro)
        {
            if (id != seguro.IdSeguro) return NotFound();

            ModelState.Remove("EmpleadoSeguros");
            ModelState.Remove("CarroSeguros");

            if (ModelState.IsValid)
            {
                if (await _context.Seguros.AnyAsync(s => s.TipoSeguro == seguro.TipoSeguro && s.IdSeguro != id))
                {
                    ModelState.AddModelError("TipoSeguro", "Ya existe otro seguro con ese nombre.");
                    return View(seguro);
                }

                try
                {
                    _context.Update(seguro);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Seguro \"{seguro.TipoSeguro}\" actualizado.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Seguros.AnyAsync(s => s.IdSeguro == id)) return NotFound();
                    throw;
                }
            }
            return View(seguro);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var seguro = await _context.Seguros.FirstOrDefaultAsync(s => s.IdSeguro == id);
            if (seguro == null) return NotFound();
            return View(seguro);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var seguro = await _context.Seguros.FirstOrDefaultAsync(s => s.IdSeguro == id);
            if (seguro == null) return NotFound();

            try
            {
                _context.Seguros.Remove(seguro);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Seguro eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: tiene carros o empleados asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
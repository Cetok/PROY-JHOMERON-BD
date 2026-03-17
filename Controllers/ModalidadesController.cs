using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class ModalidadesController : Controller
    {
        private readonly AppDbContext _context;

        public ModalidadesController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estadoFiltro, int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Modalidades
                .Include(m => m.CarroModalidades)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(m => m.TipoModalidad.Contains(buscar));

            if (!string.IsNullOrWhiteSpace(estadoFiltro))
                query = query.Where(m => m.Estado == estadoFiltro);

            int total = await query.CountAsync();

            var modalidades = await query
                .OrderBy(m => m.TipoModalidad)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.EstadoFiltro = estadoFiltro;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(modalidades);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var modalidad = await _context.Modalidades
                .Include(m => m.CarroModalidades).ThenInclude(cm => cm.Carro)
                .FirstOrDefaultAsync(m => m.IdModalidad == id);

            if (modalidad == null) return NotFound();
            return View(modalidad);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Modalidad modalidad)
        {
            ModelState.Remove("CarroModalidades");
            ModelState.Remove("Estado");

            modalidad.Estado = "Activo";

            if (ModelState.IsValid)
            {
                if (await _context.Modalidades.AnyAsync(m => m.TipoModalidad == modalidad.TipoModalidad))
                {
                    ModelState.AddModelError("TipoModalidad", "Ya existe una modalidad con ese nombre.");
                    return View(modalidad);
                }

                _context.Add(modalidad);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Modalidad \"{modalidad.TipoModalidad}\" registrada correctamente.";
                return RedirectToAction(nameof(Details), new { id = modalidad.IdModalidad });
            }
            return View(modalidad);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var modalidad = await _context.Modalidades.FirstOrDefaultAsync(m => m.IdModalidad == id);
            if (modalidad == null) return NotFound();
            return View(modalidad);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Modalidad modalidad)
        {
            if (id != modalidad.IdModalidad) return NotFound();

            ModelState.Remove("CarroModalidades");

            if (ModelState.IsValid)
            {
                if (await _context.Modalidades.AnyAsync(m => m.TipoModalidad == modalidad.TipoModalidad && m.IdModalidad != id))
                {
                    ModelState.AddModelError("TipoModalidad", "Ya existe otra modalidad con ese nombre.");
                    return View(modalidad);
                }

                try
                {
                    _context.Update(modalidad);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Modalidad \"{modalidad.TipoModalidad}\" actualizada.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Modalidades.AnyAsync(m => m.IdModalidad == id)) return NotFound();
                    throw;
                }
            }
            return View(modalidad);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var modalidad = await _context.Modalidades
                .Include(m => m.CarroModalidades)
                .FirstOrDefaultAsync(m => m.IdModalidad == id);
            if (modalidad == null) return NotFound();
            return View(modalidad);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var modalidad = await _context.Modalidades.FirstOrDefaultAsync(m => m.IdModalidad == id);
            if (modalidad == null) return NotFound();

            try
            {
                _context.Modalidades.Remove(modalidad);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Modalidad eliminada correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: tiene vehículos asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
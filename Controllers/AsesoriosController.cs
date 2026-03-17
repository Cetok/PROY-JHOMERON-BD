using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class AsesoriosController : Controller
    {
        private readonly AppDbContext _context;

        public AsesoriosController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Asesorios
                .Include(a => a.CarroAsesorios)
                .Include(a => a.GrupoAsesorios)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a => a.TipoAsesorio.Contains(buscar));

            int total = await query.CountAsync();

            var asesorios = await query
                .OrderBy(a => a.TipoAsesorio)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(asesorios);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var asesorio = await _context.Asesorios
                .Include(a => a.CarroAsesorios).ThenInclude(ca => ca.Carro)
                .Include(a => a.GrupoAsesorios).ThenInclude(ga => ga.Grupo)
                .FirstOrDefaultAsync(a => a.IdAsesorio == id);

            if (asesorio == null) return NotFound();
            return View(asesorio);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asesorio asesorio)
        {
            ModelState.Remove("CarroAsesorios");
            ModelState.Remove("GrupoAsesorios");

            if (ModelState.IsValid)
            {
                if (await _context.Asesorios.AnyAsync(a => a.TipoAsesorio == asesorio.TipoAsesorio))
                {
                    ModelState.AddModelError("TipoAsesorio", "Ya existe un accesorio con ese nombre.");
                    return View(asesorio);
                }

                _context.Add(asesorio);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Accesorio \"{asesorio.TipoAsesorio}\" registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = asesorio.IdAsesorio });
            }
            return View(asesorio);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var asesorio = await _context.Asesorios.FirstOrDefaultAsync(a => a.IdAsesorio == id);
            if (asesorio == null) return NotFound();
            return View(asesorio);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Asesorio asesorio)
        {
            if (id != asesorio.IdAsesorio) return NotFound();

            ModelState.Remove("CarroAsesorios");
            ModelState.Remove("GrupoAsesorios");

            if (ModelState.IsValid)
            {
                if (await _context.Asesorios.AnyAsync(a => a.TipoAsesorio == asesorio.TipoAsesorio && a.IdAsesorio != id))
                {
                    ModelState.AddModelError("TipoAsesorio", "Ya existe otro accesorio con ese nombre.");
                    return View(asesorio);
                }

                try
                {
                    _context.Update(asesorio);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Accesorio \"{asesorio.TipoAsesorio}\" actualizado.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Asesorios.AnyAsync(a => a.IdAsesorio == id)) return NotFound();
                    throw;
                }
            }
            return View(asesorio);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var asesorio = await _context.Asesorios
                .Include(a => a.CarroAsesorios)
                .Include(a => a.GrupoAsesorios)
                .FirstOrDefaultAsync(a => a.IdAsesorio == id);
            if (asesorio == null) return NotFound();
            return View(asesorio);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var asesorio = await _context.Asesorios.FirstOrDefaultAsync(a => a.IdAsesorio == id);
            if (asesorio == null) return NotFound();

            try
            {
                _context.Asesorios.Remove(asesorio);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Accesorio eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: tiene vehículos o grupos asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class GruposController : Controller
    {
        private readonly AppDbContext _context;

        public GruposController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? orden = "az", int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Grupos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(g => g.area != null && g.area.Contains(buscar));

            int total = await query.CountAsync();

            var grupos = await (orden == "za"
                ? query.OrderByDescending(g => g.area)
                : query.OrderBy(g => g.area))
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Orden        = orden;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.PorPagina    = porPagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(grupos);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var grupo = await _context.Grupos
                .FirstOrDefaultAsync(g => g.idGrupo == id);

            if (grupo == null) return NotFound();

            var asesorios = await _context.GrupoAsesorios
                .Include(ga => ga.Asesorio)
                .Where(ga => ga.IdGrupo == id)
                .ToListAsync();

            ViewBag.Asesorios = asesorios;
            return View(grupo);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Grupo grupo)
        {
            if (ModelState.IsValid)
            {
                bool existe = await _context.Grupos
                    .AnyAsync(g => g.area == grupo.area);

                if (existe)
                {
                    ModelState.AddModelError("area", "Ya existe un grupo con esa área.");
                    return View(grupo);
                }

                _context.Add(grupo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Grupo '{grupo.area}' registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(grupo);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var grupo = await _context.Grupos
                .FirstOrDefaultAsync(g => g.idGrupo == id);

            if (grupo == null) return NotFound();
            return View(grupo);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Grupo grupo)
        {
            if (id != grupo.idGrupo) return NotFound();

            if (ModelState.IsValid)
            {
                bool existe = await _context.Grupos
                    .AnyAsync(g => g.area == grupo.area && g.idGrupo != id);

                if (existe)
                {
                    ModelState.AddModelError("area", "Ya existe otro grupo con esa área.");
                    return View(grupo);
                }

                try
                {
                    _context.Update(grupo);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Grupo '{grupo.area}' actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Grupos.AnyAsync(g => g.idGrupo == id))
                        return NotFound();
                    throw;
                }
            }
            return View(grupo);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var grupo = await _context.Grupos
                .FirstOrDefaultAsync(g => g.idGrupo == id);

            if (grupo == null) return NotFound();
            return View(grupo);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grupo = await _context.Grupos
                .FirstOrDefaultAsync(g => g.idGrupo == id);

            if (grupo == null) return NotFound();

            try
            {
                _context.Grupos.Remove(grupo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Grupo '{grupo.area}' eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este grupo porque tiene empleados asociados.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
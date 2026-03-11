using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class EquiposController : Controller
    {
        private readonly AppDbContext _context;

        public EquiposController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estado, int? tipoId, int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Equipos
                .Include(e => e.TipoEquipo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.marca        != null && e.marca.Contains(buscar))        ||
                    (e.modelo       != null && e.modelo.Contains(buscar))       ||
                    (e.numero_serie != null && e.numero_serie.Contains(buscar)) ||
                    (e.correo_equipo!= null && e.correo_equipo.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(e => e.estado_equipo == estado);

            if (tipoId.HasValue)
                query = query.Where(e => e.idTipoEquipo == tipoId);

            int total = await query.CountAsync();

            var equipos = await query
                .OrderByDescending(e => e.fecha_compra)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Estado       = estado;
            ViewBag.TipoId       = tipoId;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
            ViewBag.Tipos        = await _context.TiposEquipo.OrderBy(t => t.tipo).ToListAsync();

            return View(equipos);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .FirstOrDefaultAsync(e => e.idEquipo == id);

            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            await CargarTipos();
            return View();
        }

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipo equipo)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(equipo.numero_serie))
                {
                    bool serieExiste = await _context.Equipos
                        .AnyAsync(e => e.numero_serie == equipo.numero_serie);
                    if (serieExiste)
                    {
                        ModelState.AddModelError("numero_serie", "Ya existe un equipo con ese número de serie.");
                        await CargarTipos(equipo.idTipoEquipo);
                        return View(equipo);
                    }
                }

                _context.Add(equipo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Equipo {equipo.marca} {equipo.modelo} registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = equipo.idEquipo });
            }

            await CargarTipos(equipo.idTipoEquipo);
            return View(equipo);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var equipo = await _context.Equipos
                .FirstOrDefaultAsync(e => e.idEquipo == id);

            if (equipo == null) return NotFound();
            await CargarTipos(equipo.idTipoEquipo);
            return View(equipo);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Equipo equipo)
        {
            if (id != equipo.idEquipo) return NotFound();

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(equipo.numero_serie))
                {
                    bool serieExiste = await _context.Equipos
                        .AnyAsync(e => e.numero_serie == equipo.numero_serie && e.idEquipo != id);
                    if (serieExiste)
                    {
                        ModelState.AddModelError("numero_serie", "Ya existe otro equipo con ese número de serie.");
                        await CargarTipos(equipo.idTipoEquipo);
                        return View(equipo);
                    }
                }

                try
                {
                    _context.Update(equipo);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Equipo {equipo.marca} {equipo.modelo} actualizado correctamente.";
                    return RedirectToAction(nameof(Details), new { id = equipo.idEquipo });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Equipos.AnyAsync(e => e.idEquipo == id))
                        return NotFound();
                    throw;
                }
            }

            await CargarTipos(equipo.idTipoEquipo);
            return View(equipo);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .FirstOrDefaultAsync(e => e.idEquipo == id);

            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var equipo = await _context.Equipos
                .FirstOrDefaultAsync(e => e.idEquipo == id);

            if (equipo == null) return NotFound();

            try
            {
                _context.Equipos.Remove(equipo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Equipo eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este equipo porque tiene registros asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // ── HELPER ───────────────────────────────────────────────
        private async Task CargarTipos(int? seleccionado = null)
        {
            var tipos = await _context.TiposEquipo
                .OrderBy(t => t.tipo).ToListAsync();
            ViewBag.TiposList = new SelectList(tipos, "idTipoEquipo", "tipo", seleccionado);
        }
    }
}
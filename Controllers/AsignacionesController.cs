using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class AsignacionesController : Controller
    {
        private readonly AppDbContext _context;

        public AsignacionesController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estado, int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo)
                .Include(a => a.Chip)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a =>
                    (a.Empleado.nombre      != null && a.Empleado.nombre.Contains(buscar))      ||
                    (a.Empleado.paterno     != null && a.Empleado.paterno.Contains(buscar))     ||
                    (a.Empleado.dni         != null && a.Empleado.dni.Contains(buscar))         ||
                    (a.Equipo.marca         != null && a.Equipo.marca.Contains(buscar))         ||
                    (a.Equipo.modelo        != null && a.Equipo.modelo.Contains(buscar))        ||
                    (a.Equipo.numero_serie  != null && a.Equipo.numero_serie.Contains(buscar))  ||
                    (a.CorreoEquipo         != null && a.CorreoEquipo.Contains(buscar))         ||
                    (a.Chip.NumeroCelular   != null && a.Chip.NumeroCelular.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(a => a.EstadoAsignacion == estado);

            int total = await query.CountAsync();

            var asignaciones = await query
                .OrderByDescending(a => a.FechaAsignacion)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Estado       = estado;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(asignaciones);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var asignacion = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Chip)
                .Include(a => a.Historiales).ThenInclude(h => h.Motivo)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asignacion == null) return NotFound();
            return View(asignacion);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            await CargarListas();
            var asignacion = new Asignacion { FechaAsignacion = DateTime.Today };
            return View(asignacion);
        }

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asignacion asignacion)
        {
            ModelState.Remove("Empleado");
            ModelState.Remove("Equipo");
            ModelState.Remove("Chip");
            ModelState.Remove("Historiales");
            ModelState.Remove("EstadoAsignacion");

            asignacion.EstadoAsignacion = "Activo";

            if (ModelState.IsValid)
            {
                bool equipoOcupado = await _context.Asignaciones
                    .AnyAsync(a => a.IdEquipo == asignacion.IdEquipo
                               && a.EstadoAsignacion == "Activo");

                if (equipoOcupado)
                {
                    ModelState.AddModelError("IdEquipo", "Este equipo ya tiene una asignación activa.");
                    await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip);
                    return View(asignacion);
                }

                if (asignacion.IdChip.HasValue)
                {
                    bool chipOcupado = await _context.Asignaciones
                        .AnyAsync(a => a.IdChip == asignacion.IdChip
                                   && a.EstadoAsignacion == "Activo");
                    if (chipOcupado)
                    {
                        ModelState.AddModelError("IdChip", "Este chip ya tiene una asignación activa.");
                        await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip);
                        return View(asignacion);
                    }
                }

                _context.Add(asignacion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Asignación registrada correctamente.";
                return RedirectToAction(nameof(Details), new { id = asignacion.IdAsignacion });
            }

            await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip);
            return View(asignacion);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var asignacion = await _context.Asignaciones
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asignacion == null) return NotFound();

            if (asignacion.EstadoAsignacion == "Inactivo")
            {
                TempData["Warning"] = "No se puede editar una asignación inactiva.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip);
            return View(asignacion);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Asignacion asignacion)
        {
            if (id != asignacion.IdAsignacion) return NotFound();

            ModelState.Remove("Empleado");
            ModelState.Remove("Equipo");
            ModelState.Remove("Chip");
            ModelState.Remove("Historiales");

            if (ModelState.IsValid)
            {
                bool equipoOcupado = await _context.Asignaciones
                    .AnyAsync(a => a.IdEquipo == asignacion.IdEquipo
                               && a.EstadoAsignacion == "Activo"
                               && a.IdAsignacion != id);
                if (equipoOcupado)
                {
                    ModelState.AddModelError("IdEquipo", "Este equipo ya tiene otra asignación activa.");
                    await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip);
                    return View(asignacion);
                }

                if (asignacion.IdChip.HasValue)
                {
                    bool chipOcupado = await _context.Asignaciones
                        .AnyAsync(a => a.IdChip == asignacion.IdChip
                                   && a.EstadoAsignacion == "Activo"
                                   && a.IdAsignacion != id);
                    if (chipOcupado)
                    {
                        ModelState.AddModelError("IdChip", "Este chip ya tiene otra asignación activa.");
                        await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip);
                        return View(asignacion);
                    }
                }

                try
                {
                    _context.Update(asignacion);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Asignación actualizada correctamente.";
                    return RedirectToAction(nameof(Details), new { id = asignacion.IdAsignacion });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Asignaciones.AnyAsync(a => a.IdAsignacion == id))
                        return NotFound();
                    throw;
                }
            }

            await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip);
            return View(asignacion);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var asignacion = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo)
                .Include(a => a.Chip)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asignacion == null) return NotFound();
            return View(asignacion);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var asignacion = await _context.Asignaciones
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asignacion == null) return NotFound();

            try
            {
                _context.Asignaciones.Remove(asignacion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Asignación eliminada correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar esta asignación porque tiene historiales registrados.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // ── HELPER ───────────────────────────────────────────────
        private async Task CargarListas(int? empleadoSel = null, int? equipoSel = null, int? chipSel = null)
        {
            var empleados = await _context.Empleados
                .Where(e => e.estado == "Activo")
                .OrderBy(e => e.paterno)
                .Select(e => new {
                    e.idEmpleado,
                    NombreCompleto = e.nombre + " " + e.paterno + " " + e.materno
                })
                .ToListAsync();

            ViewBag.EmpleadosList = new SelectList(empleados, "idEmpleado", "NombreCompleto", empleadoSel);

            var equiposOcupados = await _context.Asignaciones
                .Where(a => a.EstadoAsignacion == "Activo" && a.IdEquipo != equipoSel)
                .Select(a => a.IdEquipo)
                .ToListAsync();

            var equipos = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Where(e => e.estado_equipo == "Activo" && !equiposOcupados.Contains(e.idEquipo))
                .OrderBy(e => e.marca)
                .Select(e => new {
                    e.idEquipo,
                    Descripcion = (e.TipoEquipo != null ? e.TipoEquipo.tipo + " — " : "") +
                                  e.marca + " " + e.modelo +
                                  (e.numero_serie != null ? " [" + e.numero_serie + "]" : "")
                })
                .ToListAsync();

            ViewBag.EquiposList = new SelectList(equipos, "idEquipo", "Descripcion", equipoSel);

            var chipsOcupados = await _context.Asignaciones
                .Where(a => a.EstadoAsignacion == "Activo" && a.IdChip != chipSel && a.IdChip != null)
                .Select(a => a.IdChip!.Value)
                .ToListAsync();

            var chips = await _context.Chips
                .Where(c => !chipsOcupados.Contains(c.IdChip))
                .OrderBy(c => c.NumeroCelular)
                .ToListAsync();

            ViewBag.ChipsList = new SelectList(chips, "IdChip", "NumeroCelular", chipSel);
        }
    }
}
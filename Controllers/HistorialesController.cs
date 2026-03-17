using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class HistorialesController : Controller
    {
        private readonly AppDbContext _context;

        // Motivos que cierran la asignación E actualizan el estado del equipo
        private static readonly string[] MotivosCierre = { "Devuelto", "Perdida", "Rotura", "Baja" };

        public HistorialesController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ─────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, int? motivoId, int? asignacionId, string? orden = "za", int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Historiales
                .Include(h => h.Asignacion).ThenInclude(a => a.Empleado)
                .Include(h => h.Asignacion).ThenInclude(a => a.Equipo)
                .Include(h => h.Motivo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(h =>
                    (h.Observaciones != null && h.Observaciones.Contains(buscar)) ||
                    h.Motivo.TipoMotivo.Contains(buscar) ||
                    (h.Asignacion.Empleado.nombre  != null && h.Asignacion.Empleado.nombre.Contains(buscar))  ||
                    (h.Asignacion.Empleado.paterno != null && h.Asignacion.Empleado.paterno.Contains(buscar)));

            if (motivoId.HasValue)
                query = query.Where(h => h.IdMotivo == motivoId);

            if (asignacionId.HasValue)
                query = query.Where(h => h.IdAsignacion == asignacionId);

            int total = await query.CountAsync();

            var historiales = await (orden == "az"
                ? query.OrderBy(h => h.Fecha)
                : query.OrderByDescending(h => h.Fecha))
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            var motivos = await _context.Motivos.OrderBy(m => m.TipoMotivo).ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.MotivoId     = motivoId;
            ViewBag.AsignacionId = asignacionId;
            ViewBag.Motivos      = motivos;
            ViewBag.Orden        = orden;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            if (asignacionId.HasValue)
            {
                ViewBag.AsignacionCtx = await _context.Asignaciones
                    .Include(a => a.Empleado)
                    .Include(a => a.Equipo)
                    .FirstOrDefaultAsync(a => a.IdAsignacion == asignacionId);
            }

            return View(historiales);
        }

        // ── DETAILS ───────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var historial = await _context.Historiales
                .Include(h => h.Asignacion).ThenInclude(a => a.Empleado)
                .Include(h => h.Asignacion).ThenInclude(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(h => h.Asignacion).ThenInclude(a => a.Chip)
                .Include(h => h.Motivo)
                .FirstOrDefaultAsync(h => h.IdHistoria == id);

            if (historial == null) return NotFound();
            return View(historial);
        }

        // ── CREATE GET ─────────────────────────────────────────────
        public async Task<IActionResult> Create(int? asignacionId)
        {
            if (asignacionId.HasValue)
            {
                var asig = await _context.Asignaciones
                    .FirstOrDefaultAsync(a => a.IdAsignacion == asignacionId);

                if (asig != null && asig.EstadoAsignacion == "Inactivo")
                {
                    TempData["Warning"] = "Esta asignación ya está inactiva. No se pueden agregar más eventos.";
                    return RedirectToAction("Details", "Asignaciones", new { id = asignacionId });
                }
            }

            await CargarListas(asignacionId);
            var historial = new Historial
            {
                Fecha        = DateTime.Today,
                IdAsignacion = asignacionId ?? 0
            };
            return View(historial);
        }

        // ── CREATE POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Historial historial)
        {
            ModelState.Remove("Asignacion");
            ModelState.Remove("Motivo");

            if (ModelState.IsValid)
            {
                var motivo = await _context.Motivos
                    .FirstOrDefaultAsync(m => m.IdMotivo == historial.IdMotivo);

                bool esCierre = motivo != null && MotivosCierre.Contains(motivo.TipoMotivo);

                if (esCierre)
                {
                    // 1. Inactivar la asignación
                    var asignacion = await _context.Asignaciones
                        .FirstOrDefaultAsync(a => a.IdAsignacion == historial.IdAsignacion);

                    if (asignacion != null)
                    {
                        asignacion.EstadoAsignacion = "Inactivo";

                        // 2. Actualizar estado del equipo con el nombre del motivo
                        var equipo = await _context.Equipos
                            .FirstOrDefaultAsync(e => e.idEquipo == asignacion.IdEquipo);

                        if (equipo != null)
                            equipo.estado_equipo = motivo!.TipoMotivo; // "Devuelto", "Perdida", "Rotura" o "Baja"
                    }
                }

                _context.Add(historial);
                await _context.SaveChangesAsync();

                TempData["Success"] = esCierre
                    ? $"Evento '{motivo!.TipoMotivo}' registrado. Asignación inactiva y equipo actualizado."
                    : "Evento registrado en el historial.";

                return RedirectToAction("Details", "Asignaciones", new { id = historial.IdAsignacion });
            }

            await CargarListas(historial.IdAsignacion);
            return View(historial);
        }

        // ── EDIT GET ──────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var historial = await _context.Historiales
                .FirstOrDefaultAsync(h => h.IdHistoria == id);

            if (historial == null) return NotFound();

            await CargarListas(historial.IdAsignacion, historial.IdMotivo);
            return View(historial);
        }

        // ── EDIT POST ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Historial historial)
        {
            if (id != historial.IdHistoria) return NotFound();

            ModelState.Remove("Asignacion");
            ModelState.Remove("Motivo");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(historial);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Registro actualizado correctamente.";
                    return RedirectToAction(nameof(Details), new { id = historial.IdHistoria });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Historiales.AnyAsync(h => h.IdHistoria == id))
                        return NotFound();
                    throw;
                }
            }

            await CargarListas(historial.IdAsignacion, historial.IdMotivo);
            return View(historial);
        }

        // ── DELETE GET ────────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var historial = await _context.Historiales
                .Include(h => h.Asignacion).ThenInclude(a => a.Empleado)
                .Include(h => h.Asignacion).ThenInclude(a => a.Equipo)
                .Include(h => h.Motivo)
                .FirstOrDefaultAsync(h => h.IdHistoria == id);

            if (historial == null) return NotFound();
            return View(historial);
        }

        // ── DELETE POST ───────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var historial = await _context.Historiales
                .FirstOrDefaultAsync(h => h.IdHistoria == id);

            if (historial == null) return NotFound();

            int asigId = historial.IdAsignacion;
            _context.Historiales.Remove(historial);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Registro eliminado correctamente.";
            return RedirectToAction("Details", "Asignaciones", new { id = asigId });
        }

        // ── HELPER ────────────────────────────────────────────────
        private async Task CargarListas(int? asignacionSel = null, int? motivoSel = null)
        {
            var asignaciones = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo)
                .Where(a => a.EstadoAsignacion == "Activo")
                .OrderByDescending(a => a.FechaAsignacion)
                .Select(a => new {
                    a.IdAsignacion,
                    Descripcion = "#" + a.IdAsignacion + " — " +
                                  (a.Empleado.nombre  ?? "") + " " + (a.Empleado.paterno ?? "") +
                                  " / " + (a.Equipo.marca ?? "") + " " + (a.Equipo.modelo ?? "")
                })
                .ToListAsync();

            ViewBag.AsignacionesList = new SelectList(asignaciones, "IdAsignacion", "Descripcion", asignacionSel);

            var motivos = await _context.Motivos.OrderBy(m => m.TipoMotivo).ToListAsync();
            ViewBag.MotivosList = new SelectList(motivos, "IdMotivo", "TipoMotivo", motivoSel);
        }
    }
}
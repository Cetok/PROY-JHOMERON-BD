using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class AsignacionesController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public AsignacionesController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── INDEX ────────────────────────────────────────────────
       public async Task<IActionResult> Index(string? buscar, string? estado, int? tipoId, int pagina = 1)
        {
            int porPagina = 10;
 
            var query = _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Chip)
                .Include(a => a.Grupo)
                .AsQueryable();
 
// LUEGO agrega el filtro por tipoId DESPUÉS del filtro de estado (agrega esta línea):
            if (tipoId.HasValue)
                query = query.Where(a => a.Equipo.idTipoEquipo == tipoId);
 
// Y en los ViewBag al final agrega:
            ViewBag.TipoId       = tipoId;
            ViewBag.Tipos        = await _context.TiposEquipo.OrderBy(t => t.tipo).ToListAsync();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a =>
                    (a.Empleado.nombre     != null && a.Empleado.nombre.Contains(buscar))     ||
                    (a.Empleado.paterno    != null && a.Empleado.paterno.Contains(buscar))    ||
                    (a.Empleado.dni        != null && a.Empleado.dni.Contains(buscar))        ||
                    (a.Equipo.marca        != null && a.Equipo.marca.Contains(buscar))        ||
                    (a.Equipo.modelo       != null && a.Equipo.modelo.Contains(buscar))       ||
                    (a.Equipo.numero_serie != null && a.Equipo.numero_serie.Contains(buscar)) ||
                    (a.CorreoEquipo        != null && a.CorreoEquipo.Contains(buscar))        ||
                    (a.Chip != null && a.Chip.NumeroCelular != null && a.Chip.NumeroCelular.Contains(buscar)) ||
                    (a.Grupo != null && a.Grupo.area != null && a.Grupo.area.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(a => a.EstadoAsignacion == estado);

            int total = await query.CountAsync();

            var asignaciones = await query
                .OrderByDescending(a => a.IdAsignacion)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Estado       = estado;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
            ViewBag.PorPagina    = porPagina;

            return View(asignaciones);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var asignacion = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Chip)
                .Include(a => a.Grupo)
                .Include(a => a.Historiales).ThenInclude(h => h.Motivo)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asignacion == null) return NotFound();
            ViewBag.HistorialCambios = await _context.AuditoriaLogs
            .Where(l => l.Entidad == "Asignacion" && l.IdEntidad == id)
            .OrderByDescending(l => l.FechaHora)
            .Take(50)
            .ToListAsync();
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
            ModelState.Remove("Grupo");
            ModelState.Remove("Historiales");
            ModelState.Remove("EstadoAsignacion");

            asignacion.EstadoAsignacion = "Activo";

            if (ModelState.IsValid)
            {
                bool equipoOcupado = await _context.Asignaciones
                    .AnyAsync(a => a.IdEquipo == asignacion.IdEquipo && a.EstadoAsignacion == "Activo");

                if (equipoOcupado)
                {
                    ModelState.AddModelError("IdEquipo", "Este equipo ya tiene una asignación activa.");
                    await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
                    return View(asignacion);
                }

                if (asignacion.IdChip.HasValue)
                {
                    bool chipOcupado = await _context.Asignaciones
                        .AnyAsync(a => a.IdChip == asignacion.IdChip && a.EstadoAsignacion == "Activo");
                    if (chipOcupado)
                    {
                        ModelState.AddModelError("IdChip", "Este chip ya tiene una asignación activa.");
                        await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
                        return View(asignacion);
                    }
                }

                _context.Add(asignacion);

                // ✅ Cambiar estado del equipo a "Asignado"
                var equipo = await _context.Equipos.FindAsync(asignacion.IdEquipo);
                if (equipo != null)
                    equipo.estado_equipo = "Asignado";

                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Crear", "Asignacion", asignacion.IdAsignacion,
                    $"Registró asignación #{asignacion.IdAsignacion} — Empleado {asignacion.IdEmpleado}, Equipo {asignacion.IdEquipo}");
                await _notifService.NotificarAccionAsync("Creacion", "Asignacion",
                    $"Registró asignación #{asignacion.IdAsignacion}",
                    $"/Asignaciones/Details/{asignacion.IdAsignacion}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _as1) ? _as1 : null);

                TempData["Success"] = "Asignación registrada correctamente.";
                return RedirectToAction(nameof(Details), new { id = asignacion.IdAsignacion });
            }

            await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
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

            await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
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
            ModelState.Remove("Grupo");
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
                    await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
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
                        await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
                        return View(asignacion);
                    }
                }

                try
                {
                    var existing = await _context.Asignaciones.FindAsync(id);
                    if (existing == null) return NotFound();

                    // Si cambió el equipo, restaurar el anterior a "Activo" y marcar el nuevo como "Asignado"
                    if (existing.IdEquipo != asignacion.IdEquipo)
                    {
                        var equipoAnterior = await _context.Equipos.FindAsync(existing.IdEquipo);
                        if (equipoAnterior != null) equipoAnterior.estado_equipo = "Activo";

                        var equipoNuevo = await _context.Equipos.FindAsync(asignacion.IdEquipo);
                        if (equipoNuevo != null) equipoNuevo.estado_equipo = "Asignado";
                    }

                    existing.IdEmpleado      = asignacion.IdEmpleado;
                    existing.IdEquipo        = asignacion.IdEquipo;
                    existing.IdChip          = asignacion.IdChip;
                    existing.IdGrupo         = asignacion.IdGrupo;
                    existing.FechaAsignacion = asignacion.FechaAsignacion;
                    existing.CorreoEquipo    = asignacion.CorreoEquipo;
                    existing.NumeroCargo     = asignacion.NumeroCargo;
                    existing.EstadoAsignacion = asignacion.EstadoAsignacion;

                    await _context.SaveChangesAsync();

                     // Capturar datos anteriores
                    var asigAnterior = await _context.Asignaciones.AsNoTracking()
                        .Include(a => a.Empleado).Include(a => a.Equipo).Include(a => a.Grupo)
                        .FirstOrDefaultAsync(a => a.IdAsignacion == id);
 
                    var cambiosAsig = new List<string>();
                    if (asigAnterior != null)
                    {
                        if (asigAnterior.IdEmpleado != asignacion.IdEmpleado)
                            cambiosAsig.Add($"Empleado: '{asigAnterior.Empleado?.nombre} {asigAnterior.Empleado?.paterno}' → nuevo empleado");
                        if (asigAnterior.IdEquipo != asignacion.IdEquipo)
                            cambiosAsig.Add($"Equipo: '{asigAnterior.Equipo?.marca} {asigAnterior.Equipo?.modelo}' → nuevo equipo");
                        if (asigAnterior.IdGrupo != asignacion.IdGrupo)
                            cambiosAsig.Add($"Grupo: '{asigAnterior.Grupo?.area}' → nuevo grupo");
                        if (asigAnterior.EstadoAsignacion != asignacion.EstadoAsignacion)
                            cambiosAsig.Add($"Estado: '{asigAnterior.EstadoAsignacion}' → '{asignacion.EstadoAsignacion}'");
                        if (asigAnterior.CorreoEquipo != asignacion.CorreoEquipo)
                            cambiosAsig.Add($"Correo equipo: '{asigAnterior.CorreoEquipo ?? "—"}' → '{asignacion.CorreoEquipo ?? "—"}'");
                        if (asigAnterior.NumeroCargo != asignacion.NumeroCargo)
                            cambiosAsig.Add($"N° Cargo: '{asigAnterior.NumeroCargo ?? "—"}' → '{asignacion.NumeroCargo ?? "—"}'");
                    }
 
                    var datosAsigAnt = cambiosAsig.Any() ? string.Join(" | ", cambiosAsig) : "Sin cambios detectados";
                    await _auditoriaService.RegistrarAsync("Editar", "Asignacion", id,
                        $"Editó asignación #{id}", datosAsigAnt);
                    await _notifService.NotificarAccionAsync("Edicion", "Asignacion",
                        $"Editó asignación #{id}", $"/Asignaciones/Details/{id}",
                        idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _as2) ? _as2 : null);

                    TempData["Success"] = "Asignación actualizada correctamente.";
                    return RedirectToAction(nameof(Details), new { id = asignacion.IdAsignacion });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Asignaciones.AnyAsync(a => a.IdAsignacion == id)) return NotFound();
                    throw;
                }
            }

            await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
            return View(asignacion);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var asignacion = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo)
                .Include(a => a.Chip)
                .Include(a => a.Grupo)
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
                // ✅ Restaurar equipo a "Activo" al eliminar asignación
                var equipo = await _context.Equipos.FindAsync(asignacion.IdEquipo);
                if (equipo != null && equipo.estado_equipo == "Asignado")
                    equipo.estado_equipo = "Activo";

                _context.Asignaciones.Remove(asignacion);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Eliminar", "Asignacion", id, $"Eliminó asignación #{id}");
                await _notifService.NotificarAccionAsync("Eliminacion", "Asignacion",
                    $"Eliminó asignación #{id}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _as3) ? _as3 : null);

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
        private async Task CargarListas(int? empleadoSel = null, int? equipoSel = null,
            int? chipSel = null, int? grupoSel = null)
        {
            // Empleados activos
            var empleados = await _context.Empleados
                .Where(e => e.estado == "Activo")
                .OrderBy(e => e.paterno)
                .Select(e => new {
                    e.idEmpleado,
                    NombreCompleto = e.nombre + " " + e.paterno + " " + e.materno
                })
                .ToListAsync();
            ViewBag.EmpleadosList = new SelectList(empleados, "idEmpleado", "NombreCompleto", empleadoSel);

            // Equipos disponibles
            var equiposOcupados = await _context.Asignaciones
                .Where(a => a.EstadoAsignacion == "Activo" && a.IdEquipo != equipoSel)
                .Select(a => a.IdEquipo)
                .ToListAsync();

            var equipos = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Where(e =>
                    // Equipos disponibles (Activos y no ocupados por otra asignación)
                    (e.estado_equipo == "Activo" && !equiposOcupados.Contains(e.idEquipo))
                    // O el equipo actualmente asignado (para que aparezca en el dropdown al editar)
                    || (equipoSel.HasValue && e.idEquipo == equipoSel.Value))
                .OrderBy(e => e.marca)
                .Select(e => new {
                    e.idEquipo,
                    e.idTipoEquipo,
                    TipoNombre   = e.TipoEquipo != null ? e.TipoEquipo.tipo : "",
                    Descripcion  = (e.TipoEquipo != null ? e.TipoEquipo.tipo + " — " : "") +
                                   (e.TipoEquipo != null && e.TipoEquipo.tipo != null &&
                                    e.TipoEquipo.tipo.ToUpper().Contains("PC COMPLETO") && e.NombrePc != null
                                       ? e.NombrePc
                                       : e.marca + " " + e.modelo) +
                                   (e.numero_serie != null ? " [" + e.numero_serie + "]" : "")
                })
                .ToListAsync();

            ViewBag.EquiposList  = new SelectList(equipos, "idEquipo", "Descripcion", equipoSel);
            // JSON para detectar si el equipo seleccionado es celular en el JS
            ViewBag.EquiposJson  = System.Text.Json.JsonSerializer.Serialize(
                equipos.Select(e => new { id = e.idEquipo, tipo = e.TipoNombre })
            );

            // Chips disponibles
            var chipsOcupados = await _context.Asignaciones
                .Where(a => a.EstadoAsignacion == "Activo" && a.IdChip != chipSel && a.IdChip != null)
                .Select(a => a.IdChip!.Value)
                .ToListAsync();

            var chips = await _context.Chips
                .Where(c => !chipsOcupados.Contains(c.IdChip))
                .OrderBy(c => c.NumeroCelular)
                .ToListAsync();
            ViewBag.ChipsList = new SelectList(chips, "IdChip", "NumeroCelular", chipSel);

            // Grupos
            var grupos = await _context.Grupos
                .Where(g => g.area != null)
                .OrderBy(g => g.area)
                .ToListAsync();
            ViewBag.GruposList = new SelectList(grupos, "idGrupo", "area", grupoSel);
        }
    }
}
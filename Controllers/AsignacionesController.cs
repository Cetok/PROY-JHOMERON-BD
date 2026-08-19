using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;
using System.Text;


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

            var rolIdx = HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rolIdx == "SoporteTI")
                query = query.Where(a => a.Equipo.TipoEquipo == null ||
                    !a.Equipo.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
            else if (rolIdx == "Logistica")
                query = query.Where(a => a.Equipo.TipoEquipo != null &&
                    a.Equipo.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));

            if (tipoId.HasValue)
                query = query.Where(a => a.Equipo.idTipoEquipo == tipoId);

            ViewBag.TipoId = tipoId;
            ViewBag.Tipos  = await _context.TiposEquipo.OrderBy(t => t.tipo).ToListAsync();

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
                var rol    = HttpContext.Session.GetString("UsuarioRol") ?? "";
                var equipo = await _context.Equipos
                    .Include(e => e.TipoEquipo)
                    .FirstOrDefaultAsync(e => e.idEquipo == asignacion.IdEquipo);
                var tipoEquipo = equipo?.TipoEquipo?.tipo?.ToUpper() ?? "";

                if (rol.Equals("Logistica", StringComparison.OrdinalIgnoreCase) && !tipoEquipo.Contains("CELULAR"))
                {
                    ModelState.AddModelError("IdEquipo", "Solo puedes asignar equipos de tipo Celular.");
                    await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
                    return View(asignacion);
                }

                if (rol.Equals("SoporteTI", StringComparison.OrdinalIgnoreCase) && tipoEquipo.Contains("CELULAR"))
                {
                    ModelState.AddModelError("IdEquipo", "No tienes permiso para asignar equipos de tipo Celular.");
                    await CargarListas(asignacion.IdEmpleado, asignacion.IdEquipo, asignacion.IdChip, asignacion.IdGrupo);
                    return View(asignacion);
                }

                // ── Limpiar chip si el equipo no es celular (server-side) ──
                if (asignacion.IdChip.HasValue && !tipoEquipo.Contains("CELULAR"))
                    asignacion.IdChip = null;

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

                var equipoEstado = await _context.Equipos.FindAsync(asignacion.IdEquipo);
                if (equipoEstado != null) equipoEstado.estado_equipo = "Asignado";

                await _context.SaveChangesAsync();

                if (asignacion.IdChip.HasValue)
                {
                    var empleadoChip = await _context.Empleados.FindAsync(asignacion.IdEmpleado);
                    _context.ChipLogs.Add(new ChipLog
                    {
                        IdChip        = asignacion.IdChip.Value,
                        TipoEvento    = "Asignado",
                        Detalle       = $"Asignado a {empleadoChip?.nombre} {empleadoChip?.paterno} — equipo {equipoEstado?.marca} {equipoEstado?.modelo}",
                        Fecha         = DateTime.Now,
                        RegistradoPor = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema",
                        IdUsuario     = int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _chipLog1) ? _chipLog1 : null
                    });
                    await _context.SaveChangesAsync();
                }

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

                // ── Limpiar chip si el equipo no es celular (server-side) ──
                if (asignacion.IdChip.HasValue)
                {
                    var equipoEdit = await _context.Equipos
                        .Include(e => e.TipoEquipo)
                        .FirstOrDefaultAsync(e => e.idEquipo == asignacion.IdEquipo);
                    var tipoEdit = equipoEdit?.TipoEquipo?.tipo?.ToUpper() ?? "";
                    if (!tipoEdit.Contains("CELULAR"))
                        asignacion.IdChip = null;
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

                    if (existing.IdEquipo != asignacion.IdEquipo)
                    {
                        var equipoAnterior = await _context.Equipos.FindAsync(existing.IdEquipo);
                        if (equipoAnterior != null) equipoAnterior.estado_equipo = "Activo";

                        var equipoNuevo = await _context.Equipos.FindAsync(asignacion.IdEquipo);
                        if (equipoNuevo != null) equipoNuevo.estado_equipo = "Asignado";
                    }

                    existing.IdEmpleado       = asignacion.IdEmpleado;
                    existing.IdEquipo         = asignacion.IdEquipo;
                    existing.IdChip           = asignacion.IdChip;
                    existing.IdGrupo          = asignacion.IdGrupo;
                    existing.FechaAsignacion  = asignacion.FechaAsignacion;
                    existing.CorreoEquipo     = asignacion.CorreoEquipo;
                    existing.NumeroCargo      = asignacion.NumeroCargo;
                    existing.Observacion      = asignacion.Observacion;
                    existing.EstadoAsignacion = asignacion.EstadoAsignacion;

                    await _context.SaveChangesAsync();

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
                        if (asigAnterior.Observacion != asignacion.Observacion)
                            cambiosAsig.Add($"Observación: '{asigAnterior.Observacion ?? "—"}' → '{asignacion.Observacion ?? "—"}'");
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

        // ── HELPER: CARGAR LISTAS ─────────────────────────────────
        private async Task CargarListas(int? empleadoSel = null, int? equipoSel = null,
            int? chipSel = null, int? grupoSel = null)
        {
            var rol      = HttpContext.Session.GetString("UsuarioRol") ?? "";
            var username = HttpContext.Session.GetString("UsuarioUsername") ?? "";

            // Empleados activos
            var empleados = await _context.Empleados
                .Where(e => e.estado == "Activo")
                .OrderBy(e => e.paterno)
                .Select(e => new {
                    e.idEmpleado,
                    NombreCompleto = e.nombre + " " + e.paterno + " " + e.materno
                        + (e.Cargo != null && e.Cargo != "" ? " — " + e.Cargo : "")
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
                    (e.estado_equipo == "Activo" && !equiposOcupados.Contains(e.idEquipo))
                    || (equipoSel.HasValue && e.idEquipo == equipoSel.Value))
                .OrderBy(e => e.marca)
                .Select(e => new {
                    e.idEquipo,
                    TipoNombre  = e.TipoEquipo != null ? e.TipoEquipo.tipo : "",
                    Descripcion = (e.TipoEquipo != null ? e.TipoEquipo.tipo + " — " : "") +
                                  (e.TipoEquipo != null && e.TipoEquipo.tipo != null &&
                                   e.TipoEquipo.tipo.ToUpper().Contains("PC COMPLETO") && e.NombrePc != null
                                      ? e.NombrePc
                                      : e.marca + " " + e.modelo) +
                                  (e.numero_serie != null ? " [" + e.numero_serie + "]" : "")
                })
                .ToListAsync();

            ViewBag.EquiposList = new SelectList(equipos, "idEquipo", "Descripcion", equipoSel);
            ViewBag.EquiposJson = System.Text.Json.JsonSerializer.Serialize(
                equipos.Select(e => new { id = e.idEquipo, tipo = e.TipoNombre }));

            // ── Chips: solo Admin, Logistica y danitza (NO SoporteTI/Oliver) ──
            var puedeVerChips = rol == "Admin" || rol == "Logistica" ||
                                username.Equals("danitza", StringComparison.OrdinalIgnoreCase);

            if (puedeVerChips)
            {
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
            else
            {
                // Lista vacía — la sección chip ni se renderiza para estos roles
                ViewBag.ChipsList = new SelectList(Enumerable.Empty<object>(), "IdChip", "NumeroCelular");
            }

            ViewBag.PuedeVerChips = puedeVerChips;

            // Grupos
            var grupos = await _context.Grupos
                .Where(g => g.area != null)
                .OrderBy(g => g.area)
                .ToListAsync();
            ViewBag.GruposList = new SelectList(grupos, "idGrupo", "area", grupoSel);
        }

        // ── PDF CARGO DE EQUIPO ───────────────────────────────────
        public async Task<IActionResult> CargoPdf(int id)
        {
            var asignacion = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asignacion == null) return NotFound();

            var rolPdf  = HttpContext.Session.GetString("UsuarioRol") ?? "";
            var tipoPdf = asignacion.Equipo?.TipoEquipo?.tipo?.ToUpper() ?? "";

            if (rolPdf.Equals("Logistica", StringComparison.OrdinalIgnoreCase) && !tipoPdf.Contains("CELULAR"))
                return RedirectToAction("Denegado", "Auth");
            if (rolPdf.Equals("SoporteTI", StringComparison.OrdinalIgnoreCase) && tipoPdf.Contains("CELULAR"))
                return RedirectToAction("Denegado", "Auth");

            var username      = HttpContext.Session.GetString("UsuarioUsername") ?? "admin";
            var usuarioNombre = HttpContext.Session.GetString("UsuarioNombre") ?? "Juan Torvisco";
            var firmante = username.ToLower() switch {
                "oliver"  => "Oliver Orlando Amaricua Olivo",
                "admin"   => "Juan Torvisco",
                "danitza" => "Juan Torvisco",
                "yanet"   => usuarioNombre,
                _         => usuarioNombre
            };

            var logoPath  = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "logo.png");
            byte[]? logoBytes = System.IO.File.Exists(logoPath)
                ? await System.IO.File.ReadAllBytesAsync(logoPath) : null;

            bool esCelPdf = tipoPdf.Contains("CELULAR");
            bool usaFormatoCelular = esCelPdf && (
                rolPdf.Equals("Logistica", StringComparison.OrdinalIgnoreCase) ||
                rolPdf.Equals("Admin",     StringComparison.OrdinalIgnoreCase));
            if (usaFormatoCelular)
                return await CargoPdfCelularAsync(asignacion, firmante, logoBytes);

            var equipo         = asignacion.Equipo;
            var tipo           = equipo.TipoEquipo?.tipo?.ToUpper() ?? "";
            var empleado       = asignacion.Empleado;
            var nombreEmpleado = $"{empleado?.nombre} {empleado?.paterno} {empleado?.materno}".Trim();

            string asunto = tipo switch {
                var t when t.Contains("LAPTOP")      => "Entrega de laptop",
                var t when t.Contains("PC COMPLETO") => "Entrega de PC completo",
                var t when t.Contains("CELULAR")     => "Entrega de celular",
                var t when t.Contains("MONITOR")     => "Entrega de monitor",
                var t when t.Contains("MOUSE")       => "Entrega de mouse",
                _                                    => $"Entrega de {tipo.ToLower()}"
            };

            var nombreEquipo = tipo.Contains("PC COMPLETO") && !string.IsNullOrWhiteSpace(equipo.NombrePc)
                ? equipo.NombrePc
                : $"{equipo.marca} {equipo.modelo}".Trim();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(40); page.MarginBottom(30); page.MarginHorizontal(50);
                    page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null) row.ConstantItem(140).Image(logoBytes).FitWidth();
                            else row.ConstantItem(140).Text("JHOMERON").Bold().FontSize(20).FontColor(Color.FromHex("1A3A6B"));
                            row.RelativeItem();
                        });
                        col.Item().PaddingTop(18).PaddingBottom(6)
                            .BorderTop(1).BorderBottom(1).BorderColor(Color.FromHex("1A3A6B"))
                            .AlignCenter()
                            .Text("Cargo de Equipo").Bold().FontSize(13).FontColor(Color.FromHex("1A3A6B"));
                    });

                    page.Content().PaddingTop(24).Column(col =>
                    {
                        void Memo(string label, string valor) =>
                            col.Item().PaddingBottom(6).Row(row =>
                            {
                                row.ConstantItem(80).Text(label).Bold().FontSize(11);
                                row.RelativeItem().Text(valor).FontSize(11);
                            });

                        Memo("De:",     "Dpto. Soporte Técnico");
                        Memo("Para:",   nombreEmpleado);
                        Memo("Fecha:",  asignacion.FechaAsignacion.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-PE")));
                        Memo("Asunto:", asunto);

                        col.Item().PaddingTop(18).PaddingBottom(10)
                            .Text("Por medio de la presente se hace entrega de los siguientes equipos, los cuales quedan bajo su responsabilidad.")
                            .FontSize(11);

                        col.Item().PaddingLeft(12).PaddingBottom(6)
                            .Text($"{tipo}: {nombreEquipo}   SN: {equipo.numero_serie ?? "—"}")
                            .Bold().FontSize(11);

                        void Esp(string label, string? valor)
                        {
                            if (!string.IsNullOrWhiteSpace(valor))
                                col.Item().PaddingLeft(20).PaddingBottom(3).Row(row =>
                                {
                                    row.ConstantItem(7).Text("•").FontSize(11);
                                    row.ConstantItem(160).Text(label + ":").Bold().FontSize(11);
                                    row.RelativeItem().Text(valor).FontSize(11);
                                });
                        }

                        void SeccionTitulo(string t) =>
                            col.Item().PaddingLeft(12).PaddingTop(10).PaddingBottom(4)
                                .Text(t).Bold().FontSize(11).FontColor(Color.FromHex("1A3A6B"));

                        if (tipo.Contains("PC COMPLETO"))
                        {
                            if (!string.IsNullOrWhiteSpace(equipo.PcCpuMarca) || !string.IsNullOrWhiteSpace(equipo.PcCpuProcesador))
                            {
                                SeccionTitulo("CPU");
                                Esp("Marca",         equipo.PcCpuMarca);
                                Esp("Modelo",        equipo.PcCpuModelo);
                                Esp("N° serie",      equipo.PcCpuSerie);
                                Esp("Procesador",    equipo.PcCpuProcesador);
                                Esp("Tarjeta madre", equipo.PcCpuTarjetaMadre);
                                Esp("RAM",           equipo.PcCpuRam);
                                Esp("Disco",         equipo.PcCpuDisco);
                                Esp("Fuente",        equipo.PcCpuFuenteEnergia);
                                Esp("Gráficos",      equipo.PcCpuGraficosIntegrados == true ? "Integrados" : equipo.PcCpuTarjetaGrafica);
                                Esp("S.O.",          equipo.PcCpuSistemaOperativo);
                                Esp("Versión SO",    equipo.PcCpuVersionSO);
                            }
                            if (!string.IsNullOrWhiteSpace(equipo.PcMonitorMarca))
                            {
                                SeccionTitulo("Monitor");
                                Esp("Marca",    equipo.PcMonitorMarca);
                                Esp("Modelo",   equipo.PcMonitorModelo);
                                Esp("N° serie", equipo.PcMonitorSerie);
                            }
                            if (!string.IsNullOrWhiteSpace(equipo.PcMouseMarca))
                            {
                                SeccionTitulo("Mouse");
                                Esp("Marca",    equipo.PcMouseMarca);
                                Esp("Modelo",   equipo.PcMouseModelo);
                                Esp("N° serie", equipo.PcMouseSerie);
                                Esp("Tipo",     equipo.PcMouseEsInalambrico == true ? "Inalámbrico" : "Con cable");
                            }
                            if (!string.IsNullOrWhiteSpace(equipo.PcTecladoMarca))
                            {
                                SeccionTitulo("Teclado");
                                Esp("Marca",    equipo.PcTecladoMarca);
                                Esp("Modelo",   equipo.PcTecladoModelo);
                                Esp("N° serie", equipo.PcTecladoSerie);
                            }
                            if (!string.IsNullOrWhiteSpace(equipo.PcMousepadMarca))
                            {
                                SeccionTitulo("Mousepad");
                                Esp("Marca", equipo.PcMousepadMarca);
                            }
                        }
                        else if (tipo.Contains("LAPTOP"))
                        {
                            Esp("Procesador",    equipo.Procesador);
                            Esp("Tarjeta madre", equipo.TarjetaMadre);
                            Esp("RAM",           equipo.Ram);
                            Esp("Disco",         equipo.Disco);
                            Esp("Fuente",        equipo.FuenteEnergia);
                            Esp("Gráficos",      equipo.GraficosIntegrados == true ? "Integrados" : equipo.TarjetaGrafica);
                            Esp("S.O.",          equipo.sistema_operativo);
                            Esp("Versión SO",    equipo.version);
                        }
                        else if (tipo.Contains("CELULAR"))
                        {
                            Esp("IMEI",    equipo.IMEI);
                            Esp("S.O.",    equipo.sistema_operativo);
                            Esp("Versión", equipo.version);
                        }
                        else
                        {
                            Esp("Marca",    equipo.marca);
                            Esp("Modelo",   equipo.modelo);
                            Esp("N° serie", equipo.numero_serie);
                        }

                        if (!string.IsNullOrWhiteSpace(equipo.Observaciones))
                            col.Item().PaddingTop(12).Text($"Nota: {equipo.Observaciones}").FontSize(11).Italic();

                        col.Item().ExtendVertical().AlignBottom().Column(firmasCol =>
                        {
                            firmasCol.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().PaddingBottom(36).Text("");
                                    c.Item().BorderTop(1).BorderColor(Colors.Black).Text("").FontSize(2);
                                    c.Item().PaddingTop(4).Text("Dpto. Soporte Técnico").Bold().FontSize(11);
                                    c.Item().Text(firmante).FontSize(11);
                                });
                                row.ConstantItem(80);
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().PaddingBottom(36).Text("");
                                    c.Item().BorderTop(1).BorderColor(Colors.Black).Text("").FontSize(2);
                                    c.Item().PaddingTop(4).Text("Recibe Conforme").Bold().FontSize(11);
                                    c.Item().Text(nombreEmpleado).FontSize(11);
                                });
                            });
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Color.FromHex("1A3A6B"));
                        col.Item().PaddingTop(5).AlignCenter().Text("INDUSTRIAS JHOMERON S.A.").Bold().FontSize(9).FontColor(Color.FromHex("1A3A6B"));
                        col.Item().AlignCenter().Text("Calle Santa Ana Mza. F Lt. 44 / Fnd. Chacra Cerro - Chillón / Comas - Lima - Perú").FontSize(8).FontColor(Color.FromHex("555F7A"));
                        col.Item().AlignCenter().Text("Telfs.: 500-8202 / 500-8203 / 500-8204 / 500-8205 / 500-8206 / 500-8207 / 536-4212").FontSize(8).FontColor(Color.FromHex("555F7A"));
                    });
                });
            });

            var bytes    = pdf.GeneratePdf();
            var fileName = $"CargoEquipo_{nombreEmpleado.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        // ── PDF CELULAR ───────────────────────────────────────────
        private async Task<IActionResult> CargoPdfCelularAsync(
            PROYJHOME2026.Models.Asignacion asignacion,
            string firmante, byte[]? logoBytes)
        {
            var equipo      = asignacion.Equipo;
            var empleado    = asignacion.Empleado;
            var nombreEmp   = $"{empleado?.nombre} {empleado?.paterno} {empleado?.materno}".Trim().ToUpper();
            var dniEmp      = empleado?.dni ?? "—";
            var marca       = equipo?.marca?.ToUpper() ?? "";
            var modelo      = equipo?.modelo?.ToUpper() ?? "";
            var serie       = equipo?.numero_serie ?? "—";
            var imei        = equipo?.IMEI ?? "—";
            var so          = equipo?.sistema_operativo?.ToUpper() ?? "—";
            var version     = equipo?.version ?? "—";
            var observacion = asignacion.Observacion ?? "";
            var fechaAsig   = asignacion.FechaAsignacion;
            var fechaTexto  = $"Comas, {fechaAsig.Day} de {fechaAsig.ToString("MMMM", new System.Globalization.CultureInfo("es-PE"))} del {fechaAsig.Year}";

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(35); page.MarginBottom(30); page.MarginHorizontal(50);
                    page.DefaultTextStyle(t => t.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null) row.ConstantItem(140).Image(logoBytes).FitWidth();
                            else row.ConstantItem(140).Text("JHOMERON").Bold().FontSize(20).FontColor(Color.FromHex("1A3A6B"));
                            row.RelativeItem();
                        });
                    });

                    page.Content().PaddingTop(20).Column(col =>
                    {
                        col.Item().PaddingBottom(16)
                            .AlignCenter()
                            .Text("ENTREGA DE EQUIPO CELULAR AL PERSONAL - INDUSTRIAS JHOMERON S.A.")
                            .Bold().FontSize(13).FontColor(Colors.Black).Underline();

                        col.Item().PaddingBottom(12).Column(inner =>
                        {
                            inner.Item().PaddingBottom(6).Text("Se hace entrega a:").FontSize(11);
                            inner.Item().PaddingLeft(20).Row(row =>
                            {
                                row.AutoItem().Text(nombreEmp).Bold().FontSize(11);
                                row.AutoItem().Text("  identificado (a) con  ").FontSize(11);
                                row.AutoItem().Text($"DNI N° {dniEmp}").Bold().FontSize(11);
                            });
                        });

                        col.Item().PaddingBottom(8).Text("Lo siguiente:").FontSize(11);
                        col.Item().PaddingBottom(4).Text($"UN (01) EQUIPO {marca} {modelo},").Bold().FontSize(11);
                        col.Item().PaddingBottom(4).Text("con las siguientes características y accesorios:").FontSize(11);

                        col.Item().PaddingLeft(20).PaddingBottom(4).Row(row =>
                        {
                            row.AutoItem().Text($"{marca} {modelo}").Bold().FontSize(11);
                            row.AutoItem().Text($"  SN: {serie}  IMEI: {imei}").Bold().FontSize(11);
                        });

                        if (!string.IsNullOrWhiteSpace(so))
                        {
                            col.Item().PaddingLeft(20).PaddingBottom(2).Row(row =>
                            {
                                row.ConstantItem(6).Text("").FontSize(11);
                                row.AutoItem().Text("S.O.: ").Bold().FontSize(11);
                                row.AutoItem().Text(so).FontSize(11);
                                if (!string.IsNullOrWhiteSpace(version))
                                {
                                    row.AutoItem().Text("  Versión: ").Bold().FontSize(11);
                                    row.AutoItem().Text(version).FontSize(11);
                                }
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(observacion))
                        {
                            col.Item().PaddingTop(6).PaddingLeft(20).Column(obs =>
                            {
                                foreach (var linea in observacion.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                                    obs.Item().Text(linea.Trim()).FontSize(11);
                            });
                        }

                        col.Item().PaddingTop(20).PaddingBottom(10)
                            .Text("El equipo entregado queda BAJO ENTERA RESPONSABILIDAD del personal a quien se le hace el cargo, " +
                                  "debiendo cuidarlo y conservarlo en óptimas condiciones para el mejor desempeño de la función encomendada.")
                            .FontSize(11);

                        col.Item().PaddingBottom(10)
                            .Text("En casos de pérdida o robo, la empresa se encargará de reponer el equipo de la misma marca y modelo " +
                                  "y se descontará de su sueldo al trabajador el importe que corresponda por dicha compra, sin lugar a reclamo.")
                            .FontSize(11);

                        col.Item().PaddingBottom(20)
                            .Text("Lo que se comunica para conocimiento y fines pertinentes.").FontSize(11);

                        col.Item().PaddingBottom(30).Text(fechaTexto).FontSize(11);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingBottom(50).Text("").FontSize(11);
                                c.Item().BorderTop(1).BorderColor(Colors.Black).Text("").FontSize(2);
                                c.Item().PaddingTop(4).Text($"Sr./Sra. {nombreEmp}").Bold().FontSize(11);
                                c.Item().Text($"DNI N° {dniEmp}").FontSize(11);
                                c.Item().Text("RECIBI CONFORME").FontSize(11);
                            });
                            row.ConstantItem(40);
                            row.ConstantItem(120).Column(c =>
                            {
                                c.Item().Border(1).BorderColor(Colors.Black)
                                    .Width(110).Height(90)
                                    .AlignCenter().AlignMiddle()
                                    .Text("Huella").FontSize(9).FontColor(Color.FromHex("AAAAAA"));
                            });
                        });
                    });

                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Color.FromHex("1A3A6B"));
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("INDUSTRIAS JHOMERON S.A.").Bold().FontSize(9).FontColor(Color.FromHex("1A3A6B"));
                        });
                        col.Item().Text("Calle Santa Ana Mza. F Lt. 44 / Fnd. Chacra Cerro - Chillón / Comas - Lima - Perú").FontSize(8).FontColor(Color.FromHex("555F7A"));
                        col.Item().Text("Telfs.: 500-8202 / 500-8203 / 500-8204 / 500-8205 / 500-8206 / 500-8207 / 536-4212").FontSize(8).FontColor(Color.FromHex("555F7A"));
                    });
                });
            });

            var bytes    = pdf.GeneratePdf();
            var fileName = $"EntregaCelular_{nombreEmp.Replace(" ", "_")}_{fechaAsig:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        private FileContentResult GenerarCsv(List<string> columnas, List<List<string>> filas, string titulo)
        {
            var sb = new StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine(string.Join(";", columnas.Select(c => "\"" + c + "\"")));
            foreach (var fila in filas)
                sb.AppendLine(string.Join(";", fila.Select(v => "\"" + (v ?? "—").Replace("\"", "'") + "\"")));
        
            var bom   = new byte[] { 0xEF, 0xBB, 0xBF };
            var datos = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes = bom.Concat(datos).ToArray();
            var nombre = titulo.Replace(" ", "_") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";
            return File(bytes, "text/csv; charset=utf-8-sig", nombre);
        }
        
        private FileContentResult GenerarPdf(string titulo, List<string> columnas, List<List<string>> filas)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema";
        
            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(columnas.Count > 5 ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.MarginHorizontal(28);
                    page.MarginVertical(24);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
        
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("INDUSTRIAS JHOMERON S.A").Bold().FontSize(14).FontColor("#1e3a5f");
                                c.Item().Text(titulo).FontSize(11).FontColor("#374151");
                                c.Item().Text("Generado por: " + nombreUsuario + "  |  " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                    .FontSize(8).FontColor("#9ca3af");
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#e5e7eb");
                    });
        
                    page.Content().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(cols => { foreach (var _ in columnas) cols.RelativeColumn(); });
        
                        table.Header(header =>
                        {
                            foreach (var col in columnas)
                                header.Cell().Background("#1e3a5f").Padding(5).Text(col).Bold().FontColor("#ffffff").FontSize(8);
                        });
        
                        var alt = false;
                        foreach (var fila in filas)
                        {
                            var bg = alt ? "#f9fafb" : "#ffffff";
                            foreach (var celda in fila)
                                table.Cell().Background(bg).BorderBottom(1).BorderColor("#f3f4f6").Padding(4).Text(celda ?? "—").FontSize(8);
                            alt = !alt;
                        }
                    });
        
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Página ").FontSize(7).FontColor("#9ca3af");
                        t.CurrentPageNumber().FontSize(7).FontColor("#9ca3af");
                        t.Span(" de ").FontSize(7).FontColor("#9ca3af");
                        t.TotalPages().FontSize(7).FontColor("#9ca3af");
                        t.Span("  |  Industrias Jhomeron S.A  |  RUC: 20601777844").FontSize(7).FontColor("#9ca3af");
                    });
                });
            }).GeneratePdf();
        
            var nombre = titulo.Replace(" ", "_") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf";
            return File(bytes, "application/pdf", nombre);
        }
        private async Task<List<List<string>>> ObtenerFilasAsignaciones(string? buscar, string? estado, int? tipoId)
        {
            var query = _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Chip)
                .Include(a => a.Grupo)
                .AsQueryable();
        
            var rolIdx = HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rolIdx == "SoporteTI")
                query = query.Where(a => a.Equipo.TipoEquipo == null || !a.Equipo.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
            else if (rolIdx == "Logistica")
                query = query.Where(a => a.Equipo.TipoEquipo != null && a.Equipo.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
        
            if (tipoId.HasValue)
                query = query.Where(a => a.Equipo.idTipoEquipo == tipoId);
        
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a =>
                    (a.Empleado.nombre  != null && a.Empleado.nombre.Contains(buscar))  ||
                    (a.Empleado.paterno != null && a.Empleado.paterno.Contains(buscar)) ||
                    (a.Empleado.dni     != null && a.Empleado.dni.Contains(buscar))     ||
                    (a.Equipo.marca     != null && a.Equipo.marca.Contains(buscar))     ||
                    (a.Equipo.modelo    != null && a.Equipo.modelo.Contains(buscar))    ||
                    (a.CorreoEquipo     != null && a.CorreoEquipo.Contains(buscar))     ||
                    (a.Chip != null && a.Chip.NumeroCelular != null && a.Chip.NumeroCelular.Contains(buscar)) ||
                    (a.Grupo != null && a.Grupo.area != null && a.Grupo.area.Contains(buscar)));
        
            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(a => a.EstadoAsignacion == estado);
        
            var asignaciones = await query.OrderByDescending(a => a.IdAsignacion).ToListAsync();
        
            return asignaciones.Select(a => new List<string> {
                a.Empleado != null ? a.Empleado.nombre + " " + a.Empleado.paterno : "—",
                a.Equipo?.TipoEquipo?.tipo ?? "—",
                a.Equipo != null ? (a.Equipo.marca ?? "") + " " + (a.Equipo.modelo ?? "") : "—",
                a.Chip?.NumeroCelular ?? "—",
                a.Grupo?.area ?? "—",
                a.FechaAsignacion.ToString("dd/MM/yyyy"),
                a.EstadoAsignacion ?? "—"
            }).ToList();
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(string? buscar, string? estado, int? tipoId)
        {
            var columnas = new List<string> { "Empleado", "Tipo Equipo", "Equipo", "Chip", "Grupo", "Fecha Asignación", "Estado" };
            var filas = await ObtenerFilasAsignaciones(buscar, estado, tipoId);
            return GenerarCsv(columnas, filas, "Asignaciones");
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportarPdf(string? buscar, string? estado, int? tipoId)
        {
            var columnas = new List<string> { "Empleado", "Tipo Equipo", "Equipo", "Chip", "Grupo", "Fecha Asig.", "Estado" };
            var filas = await ObtenerFilasAsignaciones(buscar, estado, tipoId);
            return GenerarPdf("Asignaciones", columnas, filas);
        }
        // ── GET: Confirmar desactivación ─────────────────────────
        [HttpGet]
        public async Task<IActionResult> Desactivar(int id)
        {
            var asignacion = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Chip)
                .Include(a => a.Grupo)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asignacion == null) return NotFound();
            if (asignacion.EstadoAsignacion != "Activo")
            {
                TempData["Error"] = "Esta asignación ya no está activa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Motivos     = await _context.Motivos.OrderBy(m => m.TipoMotivo).ToListAsync();
            ViewBag.FechaHoy    = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
            return View(asignacion);
        }

        // ── POST: Confirmar desactivación ────────────────────────
        [HttpPost, ActionName("Desactivar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesactivarConfirmed(
            int id,
            int? idMotivoDesactivacion,
            string? observaciones,
            DateTime? fechaDevolucion)
        {
            var asignacion = await _context.Asignaciones
                .Include(a => a.Equipo)
                .Include(a => a.Chip)
                .Include(a => a.Historiales)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asignacion == null) return NotFound();
            if (asignacion.EstadoAsignacion != "Activo")
            {
                TempData["Error"] = "Esta asignación ya no está activa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var fecha = fechaDevolucion ?? DateTime.Now;

            // 1. Marcar asignación como Inactiva con fecha
            asignacion.EstadoAsignacion = "Inactivo";
            asignacion.FechaDevolucion  = fecha;

            // 2. Liberar equipo → vuelve a "Activo"
            if (asignacion.Equipo != null)
                asignacion.Equipo.estado_equipo = "Activo";

            // 3. Liberar chip si tenía
            if (asignacion.Chip != null)
            {
                var empleadoLog = await _context.Empleados.FindAsync(asignacion.IdEmpleado);
                _context.ChipLogs.Add(new ChipLog
                {
                    IdChip        = asignacion.Chip.IdChip,
                    TipoEvento    = "Desasignado",
                    Detalle       = $"Quitado de asignación con {empleadoLog?.nombre} {empleadoLog?.paterno} — equipo {asignacion.Equipo?.marca} {asignacion.Equipo?.modelo}. Chip queda disponible.",
                    Fecha         = fecha,
                    RegistradoPor = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema",
                    IdUsuario     = int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _chipLog2) ? _chipLog2 : null
                });
                asignacion.IdChip = null;
            }

            // 4. Registrar en Bitácora (Historiales) automáticamente
            if (idMotivoDesactivacion.HasValue)
            {
                _context.Historiales.Add(new Historial
                {
                    IdAsignacion  = id,
                    IdMotivo      = idMotivoDesactivacion.Value,
                    Fecha         = fecha,
                    Observaciones = observaciones?.Trim()
                });
            }
            else
            {
                // Si no eligió motivo, igual registra con el primer motivo disponible
                // o crea un registro genérico buscando el motivo "Devolución" o similar
                var motivoGenerico = await _context.Motivos
                    .FirstOrDefaultAsync(m => m.TipoMotivo.Contains("Devol") || m.TipoMotivo.Contains("devol"));
                if (motivoGenerico != null)
                {
                    _context.Historiales.Add(new Historial
                    {
                        IdAsignacion  = id,
                        IdMotivo      = motivoGenerico.IdMotivo,
                        Fecha         = fecha,
                        Observaciones = string.IsNullOrWhiteSpace(observaciones)
                            ? "Asignación desactivada"
                            : observaciones.Trim()
                    });
                }
            }

            await _context.SaveChangesAsync();

            // 5. Auditoría y notificación
            var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema";
            await _auditoriaService.RegistrarAsync("Editar", "Asignacion", id,
                $"Asignación #{id} desactivada por {nombreUsuario}. Fecha devolución: {fecha:dd/MM/yyyy HH:mm}." +
                (string.IsNullOrEmpty(observaciones) ? "" : " Obs: " + observaciones));

            await _notifService.NotificarAccionAsync("Eliminacion", "Asignacion",
                $"Asignación #{id} desactivada — equipo liberado",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int uid) ? uid : null);

            TempData["Success"] = "Asignación desactivada. El equipo fue liberado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
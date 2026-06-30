using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PROYJHOME2026.Controllers
{
    public class HistorialesController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        // Motivos que cierran la asignación E actualizan el estado del equipo
        private static readonly string[] MotivosCierre = { "Devuelto", "Perdida", "Rotura", "Baja" };

        public HistorialesController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
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

            var historiales = await query
                .OrderByDescending(h => h.Fecha)
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

                        // 2. ✅ Actualizar estado del equipo con el nombre del motivo
                        var equipo = await _context.Equipos
                            .FirstOrDefaultAsync(e => e.idEquipo == asignacion.IdEquipo);

                        if (equipo != null)
                            equipo.estado_equipo = motivo!.TipoMotivo; // "Devuelto", "Perdida", "Rotura" o "Baja"
                    }
                }

                _context.Add(historial);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Crear", "Historial", historial.IdHistoria,
                    $"Registró evento '{motivo?.TipoMotivo}' en asignación #{historial.IdAsignacion}");

                await _notifService.NotificarAccionAsync(
                    esCierre ? "Eliminacion" : "Creacion", "Historial",
                    esCierre
                        ? $"Asignación #{historial.IdAsignacion} cerrada — {motivo!.TipoMotivo}"
                        : $"Evento registrado en asignación #{historial.IdAsignacion}",
                    $"/Asignaciones/Details/{historial.IdAsignacion}");

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
                    var existing = await _context.Historiales.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.IdMotivo      = historial.IdMotivo;
                    existing.Fecha         = historial.Fecha;
                    existing.Observaciones = historial.Observaciones;

                    await _context.SaveChangesAsync();

                    await _auditoriaService.RegistrarAsync("Editar", "Historial", id,
                        $"Editó registro de historial #{id}");
                    await _notifService.NotificarAccionAsync("Edicion", "Historial",
                        $"Editó registro de historial #{id}");

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

        // ── REACTIVAR POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivar(int idHistoria, string observaciones)
        {
            var historial = await _context.Historiales
                .Include(h => h.Asignacion).ThenInclude(a => a.Equipo)
                .Include(h => h.Motivo)
                .FirstOrDefaultAsync(h => h.IdHistoria == idHistoria);

            if (historial == null) return NotFound();

            var asignacion = historial.Asignacion;
            if (asignacion == null) return NotFound();

            // Buscar o crear motivo "Reactivado"
            var motivoReactivado = await _context.Motivos
                .FirstOrDefaultAsync(m => m.TipoMotivo == "Reactivado");

            if (motivoReactivado == null)
            {
                motivoReactivado = new Motivo { TipoMotivo = "Reactivado" };
                _context.Motivos.Add(motivoReactivado);
                await _context.SaveChangesAsync();
            }

            // Nuevo registro en historial
            _context.Historiales.Add(new Historial
            {
                IdAsignacion  = asignacion.IdAsignacion,
                IdMotivo      = motivoReactivado.IdMotivo,
                Fecha         = DateTime.Now,
                Observaciones = observaciones
            });

            // ✅ Reactivar asignación
            asignacion.EstadoAsignacion = "Activo";

            // ✅ Reactivar equipo → vuelve a "Asignado" (sigue asignado al empleado)
            if (asignacion.Equipo != null)
                asignacion.Equipo.estado_equipo = "Asignado";

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("CambioEstado", "Asignacion", asignacion.IdAsignacion,
                $"Reactivó asignación #{asignacion.IdAsignacion}. Motivo: {observaciones}");

            await _notifService.NotificarAccionAsync("CambioEstado", "Asignacion",
                $"Reactivó asignación #{asignacion.IdAsignacion}",
                $"/Asignaciones/Details/{asignacion.IdAsignacion}");

            TempData["Success"] = "Asignación reactivada correctamente.";
            return RedirectToAction(nameof(Details), new { id = idHistoria });
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
        private async Task<List<List<string>>> ObtenerFilasHistoriales(string? buscar, int? motivoId, int? asignacionId)
        {
            var query = _context.Historiales
                .Include(h => h.Asignacion).ThenInclude(a => a.Empleado)
                .Include(h => h.Asignacion).ThenInclude(a => a.Equipo)
                .Include(h => h.Motivo)
                .AsQueryable();
        
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(h =>
                    (h.Observaciones != null && h.Observaciones.Contains(buscar)) ||
                    h.Motivo.TipoMotivo.Contains(buscar) ||
                    (h.Asignacion.Empleado.nombre  != null && h.Asignacion.Empleado.nombre.Contains(buscar)) ||
                    (h.Asignacion.Empleado.paterno != null && h.Asignacion.Empleado.paterno.Contains(buscar)));
        
            if (motivoId.HasValue)
                query = query.Where(h => h.IdMotivo == motivoId);
            if (asignacionId.HasValue)
                query = query.Where(h => h.IdAsignacion == asignacionId);
        
            var historiales = await query.OrderByDescending(h => h.Fecha).ToListAsync();
        
            return historiales.Select(h => new List<string> {
                h.Asignacion?.Empleado != null ? h.Asignacion.Empleado.nombre + " " + h.Asignacion.Empleado.paterno : "—",
                h.Asignacion?.Equipo != null ? (h.Asignacion.Equipo.marca ?? "") + " " + (h.Asignacion.Equipo.modelo ?? "") : "—",
                h.Motivo?.TipoMotivo ?? "—",
                h.Fecha.ToString("dd/MM/yyyy"),
                h.Observaciones ?? "—"
            }).ToList();
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(string? buscar, int? motivoId, int? asignacionId)
        {
            var columnas = new List<string> { "Empleado", "Equipo", "Motivo", "Fecha", "Observaciones" };
            var filas = await ObtenerFilasHistoriales(buscar, motivoId, asignacionId);
            return GenerarCsv(columnas, filas, "Historiales");
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportarPdf(string? buscar, int? motivoId, int? asignacionId)
        {
            var columnas = new List<string> { "Empleado", "Equipo", "Motivo", "Fecha", "Observaciones" };
            var filas = await ObtenerFilasHistoriales(buscar, motivoId, asignacionId);
            return GenerarPdf("Historiales", columnas, filas);
        }
    }
}
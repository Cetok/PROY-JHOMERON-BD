using Microsoft.AspNetCore.Mvc;
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
    public class ChipsController : Controller
    {
        private readonly AppDbContext    _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public ChipsController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? orden = "az", int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Chips.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c => c.NumeroCelular.Contains(buscar));

            int total = await query.CountAsync();

            var chips = await query.OrderByDescending(c => c.IdChip)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            // Para cada chip saber si está asignado actualmente
            var idsAsignados = await _context.Asignaciones
                .Where(a => a.IdChip != null && a.EstadoAsignacion == "Activo")
                .Select(a => a.IdChip!.Value)
                .ToListAsync();

            ViewBag.IdsAsignados = idsAsignados;
            ViewBag.Buscar       = buscar;
            ViewBag.Orden        = orden;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(chips);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var chip = await _context.Chips
                .FirstOrDefaultAsync(c => c.IdChip == id);

            if (chip == null) return NotFound();

            // Historial de asignaciones de este chip
            var asignaciones = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo)
                .Where(a => a.IdChip == id)
                .OrderByDescending(a => a.FechaAsignacion)
                .ToListAsync();

            ViewBag.Asignaciones = asignaciones;

            var logs = await _context.ChipLogs
                .Where(l => l.IdChip == id)
                .OrderByDescending(l => l.Fecha)
                .ToListAsync();
            ViewBag.Logs = logs;

            return View(chip);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chip chip)
        {
            if (ModelState.IsValid)
            {
                bool existe = await _context.Chips
                    .AnyAsync(c => c.NumeroCelular == chip.NumeroCelular);

                if (existe)
                {
                    ModelState.AddModelError("NumeroCelular", "Ya existe un chip con ese número celular.");
                    return View(chip);
                }

                _context.Add(chip);
                await _context.SaveChangesAsync();
                await _auditoriaService.RegistrarAsync("Crear", "Chip", chip.IdChip,
                    $"Registró chip {chip.NumeroCelular}");
                
                await _notifService.NotificarAccionAsync("Creacion", "Chip", $"Registró chip {chip.NumeroCelular}");
                TempData["Success"] = $"Chip {chip.NumeroCelular} registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(chip);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var chip = await _context.Chips
                .FirstOrDefaultAsync(c => c.IdChip == id);

            if (chip == null) return NotFound();
            return View(chip);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Chip chip)
        {
            if (id != chip.IdChip) return NotFound();

            if (ModelState.IsValid)
            {
                bool existe = await _context.Chips
                    .AnyAsync(c => c.NumeroCelular == chip.NumeroCelular && c.IdChip != id);

                if (existe)
                {
                    ModelState.AddModelError("NumeroCelular", "Ya existe otro chip con ese número.");
                    return View(chip);
                }

                try
                {
                    _context.Update(chip);
                    await _context.SaveChangesAsync();
                    await _auditoriaService.RegistrarAsync("Editar", "Chip", id,
                        $"Editó chip {chip.NumeroCelular}");
                    
                await _notifService.NotificarAccionAsync("Edicion", "Chip", $"Editó chip {chip.NumeroCelular}");
                TempData["Success"] = $"Chip {chip.NumeroCelular} actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Chips.AnyAsync(c => c.IdChip == id))
                        return NotFound();
                    throw;
                }
            }
            return View(chip);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var chip = await _context.Chips
                .FirstOrDefaultAsync(c => c.IdChip == id);

            if (chip == null) return NotFound();

            ViewBag.TotalAsignaciones = await _context.Asignaciones
                .CountAsync(a => a.IdChip == id);

            return View(chip);
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
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(string? buscar)
        {
            var query = _context.Chips
                .Include(c => c.Asignaciones).ThenInclude(a => a.Empleado)
                .AsQueryable();
        
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c => c.NumeroCelular.Contains(buscar));
        
            var chips = await query.OrderByDescending(c => c.IdChip).ToListAsync();
            var idsAsignados = await _context.Asignaciones
                .Where(a => a.IdChip != null && a.EstadoAsignacion == "Activo")
                .Select(a => a.IdChip!.Value).ToListAsync();
        
            var columnas = new List<string> { "Número Celular", "Estado", "Empleado Asignado", "Fecha Asignación" };
            var filas = chips.Select(c => {
                var asig = c.Asignaciones.FirstOrDefault(a => a.EstadoAsignacion == "Activo");
                return new List<string> {
                    c.NumeroCelular,
                    idsAsignados.Contains(c.IdChip) ? "Asignado" : "Disponible",
                    asig != null ? ((asig.Empleado?.nombre ?? "") + " " + (asig.Empleado?.paterno ?? "")).Trim() : "—",
                    asig?.FechaAsignacion.ToString("dd/MM/yyyy") ?? "—"
                };
            }).ToList();
        
            return GenerarCsv(columnas, filas, "Chips_SIM");
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
                                c.Item().Text("INDUSTRIAS JHOMERON S.A")
                                    .Bold().FontSize(14).FontColor("#1e3a5f");
                                c.Item().Text(titulo)
                                    .FontSize(11).FontColor("#374151");
                                c.Item().Text("Generado por: " + nombreUsuario +
                                    "  |  " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                    .FontSize(8).FontColor("#9ca3af");
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#e5e7eb");
                    });
        
                    page.Content().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            foreach (var _ in columnas) cols.RelativeColumn();
                        });
        
                        table.Header(header =>
                        {
                            foreach (var col in columnas)
                                header.Cell()
                                    .Background("#1e3a5f")
                                    .Padding(5)
                                    .Text(col)
                                    .Bold().FontColor("#ffffff").FontSize(8);
                        });
        
                        var alt = false;
                        foreach (var fila in filas)
                        {
                            var bg = alt ? "#f9fafb" : "#ffffff";
                            foreach (var celda in fila)
                                table.Cell()
                                    .Background(bg)
                                    .BorderBottom(1).BorderColor("#f3f4f6")
                                    .Padding(4)
                                    .Text(celda ?? "—")
                                    .FontSize(8);
                            alt = !alt;
                        }
                    });
        
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Página ").FontSize(7).FontColor("#9ca3af");
                        t.CurrentPageNumber().FontSize(7).FontColor("#9ca3af");
                        t.Span(" de ").FontSize(7).FontColor("#9ca3af");
                        t.TotalPages().FontSize(7).FontColor("#9ca3af");
                        t.Span("  |  Industrias Jhomeron S.A  |  RUC: 20601777844")
                            .FontSize(7).FontColor("#9ca3af");
                    });
                });
            }).GeneratePdf();
        
            var nombre = titulo.Replace(" ", "_") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf";
            return File(bytes, "application/pdf", nombre);
        }
        [HttpGet]
        public async Task<IActionResult> ExportarPdf(string? buscar)
        {
            var query = _context.Chips
                .Include(c => c.Asignaciones).ThenInclude(a => a.Empleado)
                .AsQueryable();
        
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c => c.NumeroCelular.Contains(buscar));
        
            var chips = await query.OrderByDescending(c => c.IdChip).ToListAsync();
            var idsAsignados = await _context.Asignaciones
                .Where(a => a.IdChip != null && a.EstadoAsignacion == "Activo")
                .Select(a => a.IdChip!.Value).ToListAsync();
        
            var columnas = new List<string> { "Número Celular", "Estado", "Empleado Asignado", "Fecha Asignación" };
            var filas = chips.Select(c => {
                var asig = c.Asignaciones.FirstOrDefault(a => a.EstadoAsignacion == "Activo");
                return new List<string> {
                    c.NumeroCelular,
                    idsAsignados.Contains(c.IdChip) ? "Asignado" : "Disponible",
                    asig != null ? ((asig.Empleado?.nombre ?? "") + " " + (asig.Empleado?.paterno ?? "")).Trim() : "—",
                    asig?.FechaAsignacion.ToString("dd/MM/yyyy") ?? "—"
                };
            }).ToList();
        
            return GenerarPdf("Chips SIM", columnas, filas);
        }
        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chip = await _context.Chips
                .FirstOrDefaultAsync(c => c.IdChip == id);

            if (chip == null) return NotFound();

            try
            {
                _context.Chips.Remove(chip);
                await _context.SaveChangesAsync();
                await _auditoriaService.RegistrarAsync("Eliminar", "Chip", id,
                    $"Eliminó chip {chip.NumeroCelular}");
                
                await _notifService.NotificarAccionAsync("Eliminacion", "Chip", $"Eliminó chip {chip.NumeroCelular}");
                TempData["Success"] = $"Chip {chip.NumeroCelular} eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este chip porque tiene asignaciones asociadas.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // ── INACTIVAR / DAR DE BAJA ───────────────────────────────
        // El número deja de usarse. Si el chip estaba enganchado a una
        // asignación activa (celular con empleado), se lo desengancha
        // (Asignacion.IdChip = null) — la asignación del equipo sigue
        // igual, solo pierde el chip. Todo queda en ChipLogs con fecha/hora.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inactivar(int id, string? motivo)
        {
            var chip = await _context.Chips.FirstOrDefaultAsync(c => c.IdChip == id);
            if (chip == null) return NotFound();

            if (chip.Estado == "Inactivo")
            {
                TempData["Error"] = "Este chip ya está inactivo.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema";
            var idStr         = HttpContext.Session.GetString("UsuarioId");
            int? idUsuario    = int.TryParse(idStr, out int uid) ? uid : null;
            var fecha         = DateTime.Now;

            // Si está enganchado a una asignación activa, se desengancha
            var asignacionActiva = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo)
                .FirstOrDefaultAsync(a => a.IdChip == id && a.EstadoAsignacion == "Activo");

            if (asignacionActiva != null)
            {
                var detalleDesasig = $"Desenganchado de {asignacionActiva.Empleado?.nombre} {asignacionActiva.Empleado?.paterno} — equipo {asignacionActiva.Equipo?.marca} {asignacionActiva.Equipo?.modelo}";
                asignacionActiva.IdChip = null;

                _context.ChipLogs.Add(new ChipLog
                {
                    IdChip        = id,
                    TipoEvento    = "Desasignado",
                    Detalle       = detalleDesasig,
                    Fecha         = fecha,
                    RegistradoPor = nombreUsuario,
                    IdUsuario     = idUsuario
                });
            }

            chip.Estado = "Inactivo";

            _context.ChipLogs.Add(new ChipLog
            {
                IdChip        = id,
                TipoEvento    = "Inactivo",
                Detalle       = string.IsNullOrWhiteSpace(motivo) ? "Dado de baja" : motivo.Trim(),
                Fecha         = fecha,
                RegistradoPor = nombreUsuario,
                IdUsuario     = idUsuario
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = asignacionActiva != null
                ? $"Chip {chip.NumeroCelular} dado de baja. Se quitó del equipo que lo tenía asignado."
                : $"Chip {chip.NumeroCelular} dado de baja.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
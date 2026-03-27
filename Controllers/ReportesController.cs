using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PROYJHOME2026.Controllers
{
    public class ReportesController : Controller
    {
        private readonly AppDbContext _context;

        public ReportesController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ─────────────────────────────────────────────────
        public async Task<IActionResult> Index(string tab = "equipos")
        {
            ViewBag.Tab     = tab;
            ViewBag.Tipos   = await _context.TiposEquipo.OrderBy(t => t.tipo).ToListAsync();
            ViewBag.Grupos  = await _context.Grupos.OrderBy(g => g.area).ToListAsync();
            ViewBag.Motivos = await _context.Motivos.OrderBy(m => m.TipoMotivo).ToListAsync();
            return View();
        }

        // ═══════════════════════════════════════════════════════════
        // ── EQUIPOS ────────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> EquiposData(
            DateTime? fechaDesde, DateTime? fechaHasta,
            int? tipoId, string? estado, string? buscar)
        {
            var query = _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Asignaciones.Where(a => a.EstadoAsignacion == "Activo" || a.EstadoAsignacion == "Asignado"))
                    .ThenInclude(a => a.Empleado)
                .AsQueryable();

            if (fechaDesde.HasValue) query = query.Where(e => e.fecha_compra >= fechaDesde.Value);
            if (fechaHasta.HasValue) query = query.Where(e => e.fecha_compra <= fechaHasta.Value.AddDays(1));
            if (tipoId.HasValue)     query = query.Where(e => e.idTipoEquipo == tipoId);
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(e => estados.Contains(e.estado_equipo));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.marca        != null && e.marca.Contains(buscar))        ||
                    (e.modelo       != null && e.modelo.Contains(buscar))       ||
                    (e.numero_serie != null && e.numero_serie.Contains(buscar)));

            var data = await query
                .OrderByDescending(e => e.fecha_compra)
                .Select(e => new {
                    e.idEquipo,
                    tipo        = e.TipoEquipo != null ? e.TipoEquipo.tipo : "—",
                    e.marca,
                    e.modelo,
                    e.numero_serie,
                    e.estado_equipo,
                    fechaCompra = e.fecha_compra.ToString("dd/MM/yyyy"),
                    asignado    = e.Asignaciones.Any()
                        ? e.Asignaciones.First().Empleado.nombre + " " + e.Asignaciones.First().Empleado.paterno
                        : "—",
                    procesador  = e.Procesador ?? e.PcCpuProcesador,
                    ram         = e.Ram        ?? e.PcCpuRam,
                    disco       = e.Disco      ?? e.PcCpuDisco,
                })
                .ToListAsync();

            return Json(new { total = data.Count, registros = data });
        }

        [HttpGet]
        public async Task<IActionResult> EquiposCsv(
            DateTime? fechaDesde, DateTime? fechaHasta,
            int? tipoId, string? estado, string? buscar)
        {
            var query = _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Asignaciones.Where(a => a.EstadoAsignacion == "Activo" || a.EstadoAsignacion == "Asignado"))
                    .ThenInclude(a => a.Empleado)
                .Include(e => e.ComponenteLogs.Where(l => l.TipoEvento == "Mantenimiento"))
                .AsQueryable();

            if (fechaDesde.HasValue) query = query.Where(e => e.fecha_compra >= fechaDesde.Value);
            if (fechaHasta.HasValue) query = query.Where(e => e.fecha_compra <= fechaHasta.Value.AddDays(1));
            if (tipoId.HasValue)     query = query.Where(e => e.idTipoEquipo == tipoId);
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(e => estados.Contains(e.estado_equipo));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.marca  != null && e.marca.Contains(buscar)) ||
                    (e.modelo != null && e.modelo.Contains(buscar)));

            var equipos = await query.OrderByDescending(e => e.fecha_compra).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID,Tipo,Marca,Modelo,N° Serie,Estado,Fecha Compra,Asignado A,Procesador,RAM,Disco,N° Mantenimientos");

            foreach (var e in equipos)
            {
                var asignado = e.Asignaciones.FirstOrDefault();
                var nombre   = asignado != null
                    ? $"{asignado.Empleado?.nombre} {asignado.Empleado?.paterno}".Trim()
                    : "Sin asignar";
                var proc   = e.Procesador ?? e.PcCpuProcesador ?? "—";
                var ram    = e.Ram        ?? e.PcCpuRam         ?? "—";
                var disco  = e.Disco      ?? e.PcCpuDisco       ?? "—";
                var mantes = e.ComponenteLogs.Count(l => l.TipoEvento == "Mantenimiento");

                sb.AppendLine($"{e.idEquipo}," +
                    $"\"{e.TipoEquipo?.tipo ?? "—"}\"," +
                    $"\"{e.marca ?? "—"}\"," +
                    $"\"{e.modelo ?? "—"}\"," +
                    $"\"{e.numero_serie ?? "—"}\"," +
                    $"\"{e.estado_equipo}\"," +
                    $"{e.fecha_compra:dd/MM/yyyy}," +
                    $"\"{nombre}\"," +
                    $"\"{proc}\"," +
                    $"\"{ram}\"," +
                    $"\"{disco}\"," +
                    $"{mantes}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Equipos_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> EquiposPdf(
            DateTime? fechaDesde, DateTime? fechaHasta,
            int? tipoId, string? estado, string? buscar)
        {
            var query = _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Asignaciones.Where(a => a.EstadoAsignacion == "Activo" || a.EstadoAsignacion == "Asignado"))
                    .ThenInclude(a => a.Empleado)
                .Include(e => e.ComponenteLogs.Where(l => l.TipoEvento == "Mantenimiento"))
                .AsQueryable();

            if (fechaDesde.HasValue) query = query.Where(e => e.fecha_compra >= fechaDesde.Value);
            if (fechaHasta.HasValue) query = query.Where(e => e.fecha_compra <= fechaHasta.Value.AddDays(1));
            if (tipoId.HasValue)     query = query.Where(e => e.idTipoEquipo == tipoId);
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(e => estados.Contains(e.estado_equipo));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.marca  != null && e.marca.Contains(buscar)) ||
                    (e.modelo != null && e.modelo.Contains(buscar)));

            var equipos = await query.OrderByDescending(e => e.fecha_compra).ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("REPORTE DE EQUIPOS TI")
                                    .Bold().FontSize(14).FontColor(Color.FromHex("#1a3a6b"));
                                c.Item().Text($"SG-JHOMERON — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });
                            row.ConstantItem(140).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Total: {equipos.Count} equipos")
                                    .Bold().FontSize(10).FontColor(Color.FromHex("#2563eb"));
                                if (fechaDesde.HasValue || fechaHasta.HasValue)
                                    c.Item().Text($"{fechaDesde:dd/MM/yy} — {fechaHasta:dd/MM/yy}")
                                        .FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Color.FromHex("#1a3a6b"));
                    });

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(25);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(2f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(2.2f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1f);
                            c.ConstantColumn(35);
                        });

                        static IContainer CeldaCab(IContainer c) =>
                            c.Background(Color.FromHex("#1a3a6b")).Padding(5);

                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#", "Tipo", "Marca / Modelo", "N° Serie", "Estado", "F. Compra", "Asignado a", "Procesador", "RAM", "Mant." })
                                h.Cell().Element(CeldaCab).Text(t).Bold().FontSize(8).FontColor(Colors.White);
                        });

                        for (int i = 0; i < equipos.Count; i++)
                        {
                            var e      = equipos[i];
                            var bg     = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                            var asig   = e.Asignaciones.FirstOrDefault();
                            var nombre = asig != null
                                ? $"{asig.Empleado?.nombre} {asig.Empleado?.paterno}".Trim()
                                : "Sin asignar";
                            var mantes = e.ComponenteLogs.Count(l => l.TipoEvento == "Mantenimiento");

                            IContainer Celda(IContainer c) => c.Background(bg).Padding(4);

                            var estadoColor = e.estado_equipo switch {
                                "Activo"        => Color.FromHex("#16a34a"),
                                "Asignado"      => Color.FromHex("#2563eb"),
                                "Mantenimiento" => Color.FromHex("#d97706"),
                                _               => Color.FromHex("#6b7280")
                            };

                            table.Cell().Element(Celda).Text($"{i + 1}").FontColor(Color.FromHex("#9ca3af"));
                            table.Cell().Element(Celda).Text(e.TipoEquipo?.tipo ?? "—");
                            table.Cell().Element(Celda).Text($"{e.marca} {e.modelo}").Bold();
                            table.Cell().Element(Celda).Text(e.numero_serie ?? "—").FontColor(Color.FromHex("#4b5563"));
                            table.Cell().Element(Celda).Text(e.estado_equipo).FontColor(estadoColor);
                            table.Cell().Element(Celda).Text(e.fecha_compra.ToString("dd/MM/yyyy"));
                            table.Cell().Element(Celda).Text(nombre).FontColor(Color.FromHex("#2563eb"));
                            table.Cell().Element(Celda).Text(e.Procesador ?? e.PcCpuProcesador ?? "—").FontSize(8);
                            table.Cell().Element(Celda).Text(e.Ram ?? e.PcCpuRam ?? "—").FontSize(8);
                            table.Cell().Element(Celda).AlignCenter()
                                .Text(mantes == 0 ? "—" : mantes.ToString())
                                .Bold().FontColor(mantes > 0 ? Color.FromHex("#d97706") : Color.FromHex("#9ca3af"));
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Página ").FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.CurrentPageNumber().FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.Span(" de ").FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.TotalPages().FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf", $"Equipos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // ═══════════════════════════════════════════════════════════
        // ── ASIGNACIONES ───────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> AsignacionesData(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estadoAsig, int? grupoId, string? buscar)
        {
            var query = _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Chip)
                .Include(a => a.Grupo)
                .AsQueryable();

            if (fechaDesde.HasValue) query = query.Where(a => a.FechaAsignacion >= fechaDesde.Value);
            if (fechaHasta.HasValue) query = query.Where(a => a.FechaAsignacion <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estadoAsig))
            {
                var estados = estadoAsig.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(a => estados.Contains(a.EstadoAsignacion));
            }
            if (grupoId.HasValue) query = query.Where(a => a.IdGrupo == grupoId);
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a =>
                    (a.Empleado.nombre  != null && a.Empleado.nombre.Contains(buscar))  ||
                    (a.Empleado.paterno != null && a.Empleado.paterno.Contains(buscar)) ||
                    (a.Equipo.marca     != null && a.Equipo.marca.Contains(buscar))     ||
                    (a.Equipo.modelo    != null && a.Equipo.modelo.Contains(buscar)));

            var data = await query
                .OrderByDescending(a => a.FechaAsignacion)
                .Select(a => new {
                    a.IdAsignacion,
                    empleado     = a.Empleado.nombre + " " + a.Empleado.paterno,
                    equipo       = (a.Equipo.marca ?? "") + " " + (a.Equipo.modelo ?? ""),
                    tipoEquipo   = a.Equipo.TipoEquipo != null ? a.Equipo.TipoEquipo.tipo : "—",
                    serie        = a.Equipo.numero_serie ?? "—",
                    a.EstadoAsignacion,
                    fechaAsig    = a.FechaAsignacion.ToString("dd/MM/yyyy"),
                    grupo        = a.Grupo != null ? a.Grupo.area : "—",
                    chip         = a.Chip  != null ? a.Chip.NumeroCelular : "—",
                    correoEquipo = a.CorreoEquipo ?? "—",
                })
                .ToListAsync();

            return Json(new { total = data.Count, registros = data });
        }

        [HttpGet]
        public async Task<IActionResult> AsignacionesCsv(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estadoAsig, int? grupoId, string? buscar)
        {
            var query = _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Chip)
                .Include(a => a.Grupo)
                .AsQueryable();

            if (fechaDesde.HasValue) query = query.Where(a => a.FechaAsignacion >= fechaDesde.Value);
            if (fechaHasta.HasValue) query = query.Where(a => a.FechaAsignacion <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estadoAsig))
            {
                var estados = estadoAsig.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(a => estados.Contains(a.EstadoAsignacion));
            }
            if (grupoId.HasValue) query = query.Where(a => a.IdGrupo == grupoId);
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a =>
                    (a.Empleado.nombre  != null && a.Empleado.nombre.Contains(buscar)) ||
                    (a.Empleado.paterno != null && a.Empleado.paterno.Contains(buscar)));

            var asigs = await query.OrderByDescending(a => a.FechaAsignacion).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID,Empleado,DNI,Tipo Equipo,Equipo,N° Serie,Estado,Fecha Asignación,Grupo/Área,Chip,Correo Equipo");

            foreach (var a in asigs)
            {
                sb.AppendLine($"{a.IdAsignacion}," +
                    $"\"{a.Empleado?.nombre} {a.Empleado?.paterno}\"," +
                    $"\"{a.Empleado?.dni ?? "—"}\"," +
                    $"\"{a.Equipo?.TipoEquipo?.tipo ?? "—"}\"," +
                    $"\"{a.Equipo?.marca} {a.Equipo?.modelo}\"," +
                    $"\"{a.Equipo?.numero_serie ?? "—"}\"," +
                    $"\"{a.EstadoAsignacion}\"," +
                    $"{a.FechaAsignacion:dd/MM/yyyy}," +
                    $"\"{a.Grupo?.area ?? "—"}\"," +
                    $"\"{a.Chip?.NumeroCelular ?? "—"}\"," +
                    $"\"{a.CorreoEquipo ?? "—"}\"");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Asignaciones_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> AsignacionesPdf(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estadoAsig, int? grupoId, string? buscar)
        {
            var query = _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Chip)
                .Include(a => a.Grupo)
                .AsQueryable();

            if (fechaDesde.HasValue) query = query.Where(a => a.FechaAsignacion >= fechaDesde.Value);
            if (fechaHasta.HasValue) query = query.Where(a => a.FechaAsignacion <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estadoAsig))
            {
                var estados = estadoAsig.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(a => estados.Contains(a.EstadoAsignacion));
            }
            if (grupoId.HasValue) query = query.Where(a => a.IdGrupo == grupoId);
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a =>
                    (a.Empleado.nombre  != null && a.Empleado.nombre.Contains(buscar)) ||
                    (a.Empleado.paterno != null && a.Empleado.paterno.Contains(buscar)));

            var asigs = await query.OrderByDescending(a => a.FechaAsignacion).ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("REPORTE DE ASIGNACIONES")
                                    .Bold().FontSize(14).FontColor(Color.FromHex("#1a3a6b"));
                                c.Item().Text($"SG-JHOMERON — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });
                            row.ConstantItem(140).AlignRight().Column(c =>
                                c.Item().Text($"Total: {asigs.Count} registros")
                                    .Bold().FontSize(10).FontColor(Color.FromHex("#2563eb")));
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Color.FromHex("#1a3a6b"));
                    });

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(25);
                            c.RelativeColumn(2.5f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(2f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.3f);
                        });

                        static IContainer CeldaCab(IContainer c) =>
                            c.Background(Color.FromHex("#1a3a6b")).Padding(5);

                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#", "Empleado", "Tipo", "Equipo", "N° Serie", "Estado", "F. Asignación", "Grupo/Área", "Chip" })
                                h.Cell().Element(CeldaCab).Text(t).Bold().FontSize(8).FontColor(Colors.White);
                        });

                        for (int i = 0; i < asigs.Count; i++)
                        {
                            var a  = asigs[i];
                            var bg = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                            IContainer Celda(IContainer c) => c.Background(bg).Padding(4);

                            var estadoColor = a.EstadoAsignacion switch {
                                "Activo"           => Color.FromHex("#16a34a"),
                                "Asignado"         => Color.FromHex("#2563eb"),
                                "En mantenimiento" => Color.FromHex("#d97706"),
                                _                  => Color.FromHex("#6b7280")
                            };

                            table.Cell().Element(Celda).Text($"{i + 1}").FontColor(Color.FromHex("#9ca3af"));
                            table.Cell().Element(Celda).Text($"{a.Empleado?.nombre} {a.Empleado?.paterno}").Bold();
                            table.Cell().Element(Celda).Text(a.Equipo?.TipoEquipo?.tipo ?? "—");
                            table.Cell().Element(Celda).Text($"{a.Equipo?.marca} {a.Equipo?.modelo}");
                            table.Cell().Element(Celda).Text(a.Equipo?.numero_serie ?? "—").FontSize(8);
                            table.Cell().Element(Celda).Text(a.EstadoAsignacion).FontColor(estadoColor);
                            table.Cell().Element(Celda).Text(a.FechaAsignacion.ToString("dd/MM/yyyy"));
                            table.Cell().Element(Celda).Text(a.Grupo?.area ?? "—");
                            table.Cell().Element(Celda).Text(a.Chip?.NumeroCelular ?? "—");
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Página ").FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.CurrentPageNumber().FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.Span(" de ").FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.TotalPages().FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf", $"Asignaciones_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // ═══════════════════════════════════════════════════════════
        // ── HISTORIAL ──────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> HistorialData(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? tipoEvento, int? motivoId, string? buscar)
        {
            var logsQuery = _context.EquipoComponenteLogs
                .Include(l => l.Equipo).ThenInclude(e => e.TipoEquipo)
                .AsQueryable();

            if (fechaDesde.HasValue) logsQuery = logsQuery.Where(l => l.FechaHora >= fechaDesde.Value);
            if (fechaHasta.HasValue) logsQuery = logsQuery.Where(l => l.FechaHora <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(tipoEvento))
            {
                var tipos = tipoEvento.Split(',', StringSplitOptions.RemoveEmptyEntries);
                logsQuery = logsQuery.Where(l => tipos.Contains(l.TipoEvento));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                logsQuery = logsQuery.Where(l =>
                    (l.Equipo.marca  != null && l.Equipo.marca.Contains(buscar))  ||
                    (l.Equipo.modelo != null && l.Equipo.modelo.Contains(buscar)) ||
                    (l.Componente    != null && l.Componente.Contains(buscar)));

            var logs = await logsQuery.OrderByDescending(l => l.FechaHora)
                .Select(l => new {
                    origen       = "Componente",
                    fecha        = l.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    equipo       = (l.Equipo.marca ?? "") + " " + (l.Equipo.modelo ?? ""),
                    tipo         = l.Equipo.TipoEquipo != null ? l.Equipo.TipoEquipo.tipo : "—",
                    serie        = l.Equipo.numero_serie ?? "—",
                    evento       = l.TipoEvento,
                    detalle      = l.Componente ?? "—",
                    valorAntes   = l.ValorAnterior ?? "—",
                    valorDespues = l.ValorNuevo   ?? "—",
                    obs          = l.Observaciones ?? "—",
                    usuario      = l.NombreUsuario ?? "—",
                    motivo       = "—"
                }).ToListAsync();

            var histAsigQuery = _context.Historiales
                .Include(h => h.Asignacion).ThenInclude(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(h => h.Asignacion).ThenInclude(a => a.Empleado)
                .Include(h => h.Motivo)
                .AsQueryable();

            if (fechaDesde.HasValue) histAsigQuery = histAsigQuery.Where(h => h.Fecha >= fechaDesde.Value);
            if (fechaHasta.HasValue) histAsigQuery = histAsigQuery.Where(h => h.Fecha <= fechaHasta.Value.AddDays(1));
            if (motivoId.HasValue)   histAsigQuery = histAsigQuery.Where(h => h.IdMotivo == motivoId);
            if (!string.IsNullOrWhiteSpace(buscar))
                histAsigQuery = histAsigQuery.Where(h =>
                    (h.Asignacion.Empleado.nombre != null && h.Asignacion.Empleado.nombre.Contains(buscar)) ||
                    (h.Asignacion.Equipo.marca    != null && h.Asignacion.Equipo.marca.Contains(buscar)));

            bool incluirAsig = string.IsNullOrWhiteSpace(tipoEvento) ||
                tipoEvento.Split(',').Any(t => t.Trim() == "HistorialAsig");

            var todos = new List<object>();
            todos.AddRange(logs.Cast<object>());

            if (incluirAsig)
            {
                var histAsig = await histAsigQuery.OrderByDescending(h => h.Fecha)
                    .Select(h => new {
                        origen       = "Asignación",
                        fecha        = h.Fecha.ToString("dd/MM/yyyy HH:mm"),
                        equipo       = (h.Asignacion.Equipo.marca ?? "") + " " + (h.Asignacion.Equipo.modelo ?? ""),
                        tipo         = h.Asignacion.Equipo.TipoEquipo != null ? h.Asignacion.Equipo.TipoEquipo.tipo : "—",
                        serie        = h.Asignacion.Equipo.numero_serie ?? "—",
                        evento       = "Movimiento",
                        detalle      = h.Asignacion.Empleado.nombre + " " + h.Asignacion.Empleado.paterno,
                        valorAntes   = "—",
                        valorDespues = "—",
                        obs          = h.Observaciones ?? "—",
                        usuario      = "—",
                        motivo       = h.Motivo.TipoMotivo
                    }).ToListAsync();
                todos.AddRange(histAsig.Cast<object>());
            }

            return Json(new { total = todos.Count, registros = todos });
        }

        [HttpGet]
        public async Task<IActionResult> HistorialCsv(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? tipoEvento, int? motivoId, string? buscar)
        {
            var logsQuery = _context.EquipoComponenteLogs
                .Include(l => l.Equipo).ThenInclude(e => e.TipoEquipo)
                .AsQueryable();

            if (fechaDesde.HasValue) logsQuery = logsQuery.Where(l => l.FechaHora >= fechaDesde.Value);
            if (fechaHasta.HasValue) logsQuery = logsQuery.Where(l => l.FechaHora <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(tipoEvento))
            {
                var tipos = tipoEvento.Split(',', StringSplitOptions.RemoveEmptyEntries);
                logsQuery = logsQuery.Where(l => tipos.Contains(l.TipoEvento));
            }

            var logs = await logsQuery.OrderByDescending(l => l.FechaHora).ToListAsync();

            var histAsigs = await _context.Historiales
                .Include(h => h.Asignacion).ThenInclude(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(h => h.Asignacion).ThenInclude(a => a.Empleado)
                .Include(h => h.Motivo)
                .Where(h =>
                    (!fechaDesde.HasValue || h.Fecha >= fechaDesde.Value) &&
                    (!fechaHasta.HasValue || h.Fecha <= fechaHasta.Value.AddDays(1)) &&
                    (!motivoId.HasValue   || h.IdMotivo == motivoId))
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Origen,Fecha,Tipo Equipo,Equipo,N° Serie,Evento,Detalle,Valor Anterior,Valor Nuevo,Motivo,Observaciones,Usuario");

            foreach (var l in logs)
            {
                sb.AppendLine($"Componente," +
                    $"{l.FechaHora:dd/MM/yyyy HH:mm}," +
                    $"\"{l.Equipo?.TipoEquipo?.tipo ?? "—"}\"," +
                    $"\"{l.Equipo?.marca} {l.Equipo?.modelo}\"," +
                    $"\"{l.Equipo?.numero_serie ?? "—"}\"," +
                    $"\"{l.TipoEvento}\"," +
                    $"\"{l.Componente ?? "—"}\"," +
                    $"\"{l.ValorAnterior ?? "—"}\"," +
                    $"\"{l.ValorNuevo ?? "—"}\"," +
                    $"—," +
                    $"\"{l.Observaciones ?? "—"}\"," +
                    $"\"{l.NombreUsuario ?? "—"}\"");
            }

            foreach (var h in histAsigs)
            {
                sb.AppendLine($"Asignación," +
                    $"{h.Fecha:dd/MM/yyyy HH:mm}," +
                    $"\"{h.Asignacion?.Equipo?.TipoEquipo?.tipo ?? "—"}\"," +
                    $"\"{h.Asignacion?.Equipo?.marca} {h.Asignacion?.Equipo?.modelo}\"," +
                    $"\"{h.Asignacion?.Equipo?.numero_serie ?? "—"}\"," +
                    $"Movimiento," +
                    $"\"{h.Asignacion?.Empleado?.nombre} {h.Asignacion?.Empleado?.paterno}\"," +
                    $"—,—," +
                    $"\"{h.Motivo?.TipoMotivo ?? "—"}\"," +
                    $"\"{h.Observaciones ?? "—"}\"," +
                    $"—");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Historial_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> HistorialPdf(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? tipoEvento, int? motivoId, string? buscar)
        {
            var logs = await _context.EquipoComponenteLogs
                .Include(l => l.Equipo).ThenInclude(e => e.TipoEquipo)
                .Where(l =>
                    (!fechaDesde.HasValue || l.FechaHora >= fechaDesde.Value) &&
                    (!fechaHasta.HasValue || l.FechaHora <= fechaHasta.Value.AddDays(1)))
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();

            var histAsigs = await _context.Historiales
                .Include(h => h.Asignacion).ThenInclude(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(h => h.Asignacion).ThenInclude(a => a.Empleado)
                .Include(h => h.Motivo)
                .Where(h =>
                    (!fechaDesde.HasValue || h.Fecha >= fechaDesde.Value) &&
                    (!fechaHasta.HasValue || h.Fecha <= fechaHasta.Value.AddDays(1)) &&
                    (!motivoId.HasValue   || h.IdMotivo == motivoId))
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(8.5f).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("REPORTE DE HISTORIAL")
                                    .Bold().FontSize(14).FontColor(Color.FromHex("#1a3a6b"));
                                c.Item().Text($"SG-JHOMERON — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });
                            row.ConstantItem(170).AlignRight().Column(c =>
                                c.Item().Text($"Cambios: {logs.Count} | Movimientos: {histAsigs.Count}")
                                    .Bold().FontSize(9).FontColor(Color.FromHex("#2563eb")));
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Color.FromHex("#1a3a6b"));
                    });

                    page.Content().PaddingTop(12).Column(col =>
                    {
                        if (logs.Any())
                        {
                            col.Item().Text("Cambios de componentes y mantenimientos")
                                .Bold().FontSize(10).FontColor(Color.FromHex("#1a3a6b"));

                            col.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(1.2f);
                                });

                                static IContainer H(IContainer c) =>
                                    c.Background(Color.FromHex("#1a3a6b")).Padding(4);

                                table.Header(h =>
                                {
                                    foreach (var t in new[] { "Fecha", "Tipo", "Equipo", "Evento", "Componente", "Anterior", "Nuevo", "Observación", "Usuario" })
                                        h.Cell().Element(H).Text(t).Bold().FontSize(7.5f).FontColor(Colors.White);
                                });

                                for (int i = 0; i < logs.Count; i++)
                                {
                                    var l  = logs[i];
                                    var bg = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                                    IContainer C(IContainer c) => c.Background(bg).Padding(3);

                                    table.Cell().Element(C).Text(l.FechaHora.ToString("dd/MM/yy HH:mm")).FontSize(7.5f);
                                    table.Cell().Element(C).Text(l.Equipo?.TipoEquipo?.tipo ?? "—");
                                    table.Cell().Element(C).Text($"{l.Equipo?.marca} {l.Equipo?.modelo}").Bold();
                                    table.Cell().Element(C).Text(l.TipoEvento)
                                        .FontColor(l.TipoEvento == "Mantenimiento"
                                            ? Color.FromHex("#d97706") : Color.FromHex("#2563eb"));
                                    table.Cell().Element(C).Text(l.Componente ?? "—");
                                    table.Cell().Element(C).Text(l.ValorAnterior ?? "—").FontColor(Color.FromHex("#9ca3af"));
                                    table.Cell().Element(C).Text(l.ValorNuevo    ?? "—").FontColor(Color.FromHex("#2563eb"));
                                    table.Cell().Element(C).Text(l.Observaciones ?? "—").FontSize(7.5f);
                                    table.Cell().Element(C).Text(l.NombreUsuario ?? "—").FontSize(7.5f);
                                }
                            });
                        }

                        if (histAsigs.Any())
                        {
                            col.Item().PaddingTop(16).Text("Movimientos de asignaciones")
                                .Bold().FontSize(10).FontColor(Color.FromHex("#1a3a6b"));

                            col.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(2.5f);
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(2f);
                                });

                                static IContainer H2(IContainer c) =>
                                    c.Background(Color.FromHex("#374151")).Padding(4);

                                table.Header(h =>
                                {
                                    foreach (var t in new[] { "Fecha", "Tipo Equipo", "Equipo", "Empleado", "Motivo", "Observación" })
                                        h.Cell().Element(H2).Text(t).Bold().FontSize(7.5f).FontColor(Colors.White);
                                });

                                for (int i = 0; i < histAsigs.Count; i++)
                                {
                                    var h  = histAsigs[i];
                                    var bg = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                                    IContainer C(IContainer c) => c.Background(bg).Padding(3);

                                    table.Cell().Element(C).Text(h.Fecha.ToString("dd/MM/yy HH:mm")).FontSize(7.5f);
                                    table.Cell().Element(C).Text(h.Asignacion?.Equipo?.TipoEquipo?.tipo ?? "—");
                                    table.Cell().Element(C).Text($"{h.Asignacion?.Equipo?.marca} {h.Asignacion?.Equipo?.modelo}").Bold();
                                    table.Cell().Element(C).Text($"{h.Asignacion?.Empleado?.nombre} {h.Asignacion?.Empleado?.paterno}");
                                    table.Cell().Element(C).Text(h.Motivo?.TipoMotivo ?? "—").FontColor(Color.FromHex("#7c3aed"));
                                    table.Cell().Element(C).Text(h.Observaciones ?? "—").FontSize(7.5f);
                                }
                            });
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Página ").FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.CurrentPageNumber().FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.Span(" de ").FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                        t.TotalPages().FontSize(8).FontColor(Color.FromHex("#9ca3af"));
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf", $"Historial_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // ═══════════════════════════════════════════════════════════
        // ── DASHBOARD ──────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> DashboardData(DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var porEstado = await _context.Equipos
                .GroupBy(e => e.estado_equipo)
                .Select(g => new { estado = g.Key, total = g.Count() })
                .ToListAsync();

            var porTipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .GroupBy(e => e.TipoEquipo != null ? e.TipoEquipo.tipo : "Sin tipo")
                .Select(g => new { tipo = g.Key, total = g.Count() })
                .OrderByDescending(g => g.total)
                .ToListAsync();

            var porGrupo = await _context.Asignaciones
                .Include(a => a.Grupo)
                .Where(a => a.EstadoAsignacion == "Activo" || a.EstadoAsignacion == "Asignado")
                .GroupBy(a => a.Grupo != null ? a.Grupo.area : "Sin área")
                .Select(g => new { grupo = g.Key, total = g.Count() })
                .OrderByDescending(g => g.total)
                .ToListAsync();

            var hace12Inicio = new DateTime(DateTime.Now.AddMonths(-11).Year, DateTime.Now.AddMonths(-11).Month, 1);

            var mantesPorMes = await _context.EquipoComponenteLogs
                .Where(l => l.TipoEvento == "Mantenimiento" && l.FechaHora >= hace12Inicio)
                .GroupBy(l => new { l.FechaHora.Year, l.FechaHora.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, total = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            var mesesCompletos = Enumerable.Range(0, 12).Select(i => {
                var d     = hace12Inicio.AddMonths(i);
                var found = mantesPorMes.FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month);
                return new { mes = $"{d:MM}/{d.Year}", total = found?.total ?? 0 };
            }).ToList();

            var topMantes = await _context.EquipoComponenteLogs
                .Include(l => l.Equipo).ThenInclude(e => e.TipoEquipo)
                .Where(l => l.TipoEvento == "Mantenimiento")
                .GroupBy(l => new { l.IdEquipo, l.Equipo.marca, l.Equipo.modelo, tipo = l.Equipo.TipoEquipo != null ? l.Equipo.TipoEquipo.tipo : "—" })
                .Select(g => new { g.Key.IdEquipo, g.Key.marca, g.Key.modelo, g.Key.tipo, total = g.Count() })
                .OrderByDescending(g => g.total)
                .Take(10)
                .ToListAsync();

            var totalEquipos      = await _context.Equipos.CountAsync();
            var totalAsignados    = await _context.Equipos.CountAsync(e => e.estado_equipo == "Asignado" || e.estado_equipo == "Activo");
            var totalMante        = await _context.Equipos.CountAsync(e => e.estado_equipo == "Mantenimiento");
            var totalAsignaciones = await _context.Asignaciones.CountAsync(a => a.EstadoAsignacion == "Activo" || a.EstadoAsignacion == "Asignado");

            return Json(new {
                resumen = new { totalEquipos, totalAsignados, totalMante, totalAsignaciones },
                porEstado,
                porTipo,
                porGrupo,
                mantesPorMes = mesesCompletos,
                topMantes
            });
        }

        public IActionResult Dashboard()
        {
            ViewData["Title"]      = "Dashboard de Equipos";
            ViewData["Breadcrumb"] = "Reportes / Dashboard";
            return View();
        }
    }
}
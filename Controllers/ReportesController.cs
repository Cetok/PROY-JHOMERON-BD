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
        // Componentes que cuando se filtran por tipo, también muestran PC Completo
        private static readonly string[] ComponentesPcCompleto =
            { "CPU", "MONITOR", "MOUSE", "TECLADO", "MOUSEPAD" };
 
        private bool EsComponentePcCompleto(string? tipoNombre) =>
            tipoNombre != null &&
            ComponentesPcCompleto.Any(c => tipoNombre.ToUpper().Contains(c)) &&
            !tipoNombre.ToUpper().Contains("PC COMPLETO");

        // ═══════════════════════════════════════════════════════════
        // ── EQUIPOS ────────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> EquiposData(
            DateTime? fechaDesde, DateTime? fechaHasta,
            int? tipoId, int? grupoId, string? estado, string? buscar)
        {
            string? tipoNombre = null;
            bool incluirPcCompleto = false;
            int? tipoIdPcCompleto = null;
            if (tipoId.HasValue)
            {
                var tipoObj = await _context.TiposEquipo.FindAsync(tipoId.Value);
                tipoNombre = tipoObj?.tipo;
                incluirPcCompleto = EsComponentePcCompleto(tipoNombre);
                if (incluirPcCompleto)
                {
                    var pcObj = await _context.TiposEquipo
                        .FirstOrDefaultAsync(t => t.tipo != null && t.tipo.ToUpper().Contains("PC COMPLETO"));
                    tipoIdPcCompleto = pcObj?.idTipoEquipo;
                }
            }

            // Siempre arrancamos con una query base
            var query = _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Asignaciones.Where(a => a.EstadoAsignacion == "Activo" || a.EstadoAsignacion == "Asignado"))
                    .ThenInclude(a => a.Empleado)
                .AsQueryable();

            if (tipoId.HasValue)
            {
                if (incluirPcCompleto && tipoIdPcCompleto.HasValue)
                    // Mostrar el tipo específico (ej: CPU) Y los PC Completo
                    query = query.Where(e => e.idTipoEquipo == tipoId || e.idTipoEquipo == tipoIdPcCompleto);
                else
                    query = query.Where(e => e.idTipoEquipo == tipoId);
            }
            if (grupoId.HasValue)
                query = query.Where(e => e.Asignaciones.Any(a =>
                    (a.EstadoAsignacion == "Activo" || a.EstadoAsignacion == "Asignado") &&
                    a.IdGrupo == grupoId));

            if (fechaDesde.HasValue) query = query.Where(e => e.fecha_compra >= fechaDesde.Value);
            if (fechaHasta.HasValue) query = query.Where(e => e.fecha_compra <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(e => estados.Contains(e.estado_equipo));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.NombrePc        != null && e.NombrePc.Contains(buscar))        ||
                    (e.marca           != null && e.marca.Contains(buscar))            ||
                    (e.modelo          != null && e.modelo.Contains(buscar))           ||
                    (e.numero_serie    != null && e.numero_serie.Contains(buscar))     ||
                    (e.PcCpuMarca      != null && e.PcCpuMarca.Contains(buscar))       ||
                    (e.PcCpuModelo     != null && e.PcCpuModelo.Contains(buscar))      ||
                    (e.PcMonitorMarca  != null && e.PcMonitorMarca.Contains(buscar))   ||
                    (e.PcMonitorModelo != null && e.PcMonitorModelo.Contains(buscar))  ||
                    (e.PcMouseMarca    != null && e.PcMouseMarca.Contains(buscar))     ||
                    (e.PcTecladoMarca  != null && e.PcTecladoMarca.Contains(buscar))   ||
                    (e.PcMousepadMarca != null && e.PcMousepadMarca.Contains(buscar)));

            var equipos = await query.OrderByDescending(e => e.fecha_compra).ToListAsync();

            // Retornamos todos los equipos en un solo formato unificado.
            // Cada registro indica si es PC Completo para que la vista lo renderice correctamente.
            var data = equipos.Select(e => {
                var esPc = e.TipoEquipo?.tipo?.ToUpper().Contains("PC COMPLETO") == true;
                var asig = e.Asignaciones.FirstOrDefault();
                return new {
                    e.idEquipo,
                    tipo          = e.TipoEquipo?.tipo ?? "—",
                    // nombre: PC Completo usa NombrePc; el resto usa marca+modelo
                    nombre        = esPc ? (e.NombrePc ?? "Sin nombre") : ((e.marca ?? "") + " " + (e.modelo ?? "")).Trim(),
                    e.NombrePc,
                    e.marca,
                    e.modelo,
                    e.numero_serie,
                    e.estado_equipo,
                    fechaCompra   = e.fecha_compra.ToString("dd/MM/yyyy"),
                    asignado      = asig != null ? $"{asig.Empleado?.nombre} {asig.Empleado?.paterno}".Trim() : "—",
                    procesador    = esPc ? (e.PcCpuProcesador ?? "—") : (e.Procesador ?? "—"),
                    ram           = esPc ? (e.PcCpuRam        ?? "—") : (e.Ram        ?? "—"),
                    disco         = esPc ? (e.PcCpuDisco      ?? "—") : (e.Disco      ?? "—"),
                    so            = esPc ? (e.PcCpuSistemaOperativo ?? "—") : (e.sistema_operativo ?? "—"),
                    version       = esPc ? (e.PcCpuVersionSO  ?? "—") : (e.version    ?? "—"),
                    observaciones = e.Observaciones ?? "—",
                    esPcCompleto  = esPc,
                    // Campos extra PC Completo (la vista los usa si esPcCompleto==true)
                    cpuMarca      = e.PcCpuMarca      ?? "—",
                    cpuModelo     = e.PcCpuModelo     ?? "—",
                    cpuSerie      = e.PcCpuSerie      ?? "—",
                    monitorMarca  = e.PcMonitorMarca  ?? "—",
                    monitorModelo = e.PcMonitorModelo ?? "—",
                    monitorSerie  = e.PcMonitorSerie  ?? "—",
                    mouseMarca    = e.PcMouseMarca    ?? "—",
                    mouseModelo   = e.PcMouseModelo   ?? "—",
                    mouseSerie    = e.PcMouseSerie    ?? "—",
                    mouseInal     = e.PcMouseEsInalambrico == true ? "Inalámbrico"
                                  : e.PcMouseEsInalambrico == false ? "Con cable" : "—",
                    tecladoMarca  = e.PcTecladoMarca  ?? "—",
                    tecladoModelo = e.PcTecladoModelo ?? "—",
                    tecladoSerie  = e.PcTecladoSerie  ?? "—",
                    mousepadMarca = e.PcMousepadMarca ?? "—",
                    componenteFiltrado = tipoNombre ?? "Todos"
                };
            }).ToList();

            return Json(new { total = data.Count, registros = data, esPcCompleto = incluirPcCompleto });
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
                    (e.modelo != null && e.modelo.Contains(buscar)) ||
                    (e.NombrePc != null && e.NombrePc.Contains(buscar)));
 
            var equipos = await query.OrderByDescending(e => e.fecha_compra).ToListAsync();
 
            var sb = new StringBuilder();
            sb.AppendLine("sep=;");
 
            // Cabecera sin columnas de componentes técnicos
            sb.AppendLine("ID;Tipo;Nombre/Marca;Modelo;N° Serie;Estado;Fecha Compra;Asignado A;Observaciones;N° Mantenimientos");
 
            foreach (var e in equipos)
            {
                var esPc   = e.TipoEquipo?.tipo?.ToUpper().Contains("PC COMPLETO") == true;
                var asig   = e.Asignaciones.FirstOrDefault();
                var nombre = asig != null
                    ? $"{asig.Empleado?.nombre} {asig.Empleado?.paterno}".Trim()
                    : "Sin asignar";
                var mantes = e.ComponenteLogs.Count(l => l.TipoEvento == "Mantenimiento");
 
                // Para PC Completo: NombrePc en vez de marca/modelo/serie
                var displayNombre = esPc ? (e.NombrePc ?? "Sin nombre") : (e.marca ?? "—");
                var displayModelo = esPc ? "—" : (e.modelo ?? "—");
                var displaySerie  = esPc ? "—" : (e.numero_serie ?? "—");
 
                sb.AppendLine($"{e.idEquipo};" +
                    $"\"{e.TipoEquipo?.tipo ?? "—"}\";" +
                    $"\"{displayNombre}\";" +
                    $"\"{displayModelo}\";" +
                    $"\"{displaySerie}\";" +
                    $"\"{e.estado_equipo}\";" +
                    $"{e.fecha_compra:dd/MM/yyyy};" +
                    $"\"{nombre}\";" +
                    $"\"{e.Observaciones ?? "—"}\";" +
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
                    (e.modelo != null && e.modelo.Contains(buscar)) ||
                    (e.NombrePc != null && e.NombrePc.Contains(buscar)));
 
            var equipos = await query.OrderByDescending(e => e.fecha_compra).ToListAsync();
 
            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(9));
 
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
                        // Columnas: # | Tipo | Nombre/Marca Modelo | N°Serie | Estado | F.Compra | Asignado | Observaciones | Mant.
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(25);   // #
                            c.RelativeColumn(1.2f); // Tipo
                            c.RelativeColumn(2.2f); // Nombre/Marca Modelo
                            c.RelativeColumn(1.8f); // N° Serie
                            c.RelativeColumn(1.2f); // Estado
                            c.RelativeColumn(1.2f); // F.Compra
                            c.RelativeColumn(2f);   // Asignado a
                            c.RelativeColumn(2.5f); // Observaciones
                            c.ConstantColumn(35);   // Mant.
                        });
 
                        static IContainer CeldaCab(IContainer c) =>
                            c.Background(Color.FromHex("#1a3a6b")).Padding(5);
 
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#", "Tipo", "Nombre / Marca Modelo", "N° Serie", "Estado", "F. Compra", "Asignado a", "Observaciones", "Mant." })
                                h.Cell().Element(CeldaCab).Text(t).Bold().FontSize(8).FontColor(Colors.White);
                        });
 
                        for (int i = 0; i < equipos.Count; i++)
                        {
                            var e      = equipos[i];
                            var bg     = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                            var esPc   = e.TipoEquipo?.tipo?.ToUpper().Contains("PC COMPLETO") == true;
                            var asig   = e.Asignaciones.FirstOrDefault();
                            var nombre = asig != null
                                ? $"{asig.Empleado?.nombre} {asig.Empleado?.paterno}".Trim()
                                : "Sin asignar";
                            var mantes = e.ComponenteLogs?.Count(l => l.TipoEvento == "Mantenimiento") ?? 0;
 
                            // Para PC Completo: solo NombrePc, sin marca/modelo ni serie
                            var displayNombreModelo = esPc
                                ? (e.NombrePc ?? "Sin nombre")
                                : $"{e.marca ?? "—"} {e.modelo ?? ""}".Trim();
                            var displaySerie = esPc ? "—" : (e.numero_serie ?? "—");
 
                            IContainer Celda(IContainer c) => c.Background(bg).Padding(4);
 
                            var estadoColor = e.estado_equipo switch {
                                "Activo"        => Color.FromHex("#16a34a"),
                                "Asignado"      => Color.FromHex("#2563eb"),
                                "Mantenimiento" => Color.FromHex("#d97706"),
                                _               => Color.FromHex("#6b7280")
                            };
 
                            table.Cell().Element(Celda).Text($"{i + 1}").FontColor(Color.FromHex("#9ca3af"));
                            table.Cell().Element(Celda).Text(e.TipoEquipo?.tipo ?? "—");
                            table.Cell().Element(Celda).Text(displayNombreModelo).Bold();
                            table.Cell().Element(Celda).Text(displaySerie).FontColor(Color.FromHex("#4b5563")).FontSize(8);
                            table.Cell().Element(Celda).Text(e.estado_equipo).FontColor(estadoColor);
                            table.Cell().Element(Celda).Text(e.fecha_compra.ToString("dd/MM/yyyy")).FontSize(8);
                            table.Cell().Element(Celda).Text(nombre).FontColor(Color.FromHex("#2563eb")).FontSize(8);
                            table.Cell().Element(Celda).Text(e.Observaciones ?? "—").FontSize(7.5f).FontColor(Color.FromHex("#4b5563"));
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
            string? estadoAsig, int? grupoId, int? tipoId, string? buscar)
        {
            // Si el tipo seleccionado es un componente de PC Completo (CPU, Monitor, etc.)
            // también incluir los equipos de tipo "PC Completo"
            int? tipoIdPcCompleto = null;
            if (tipoId.HasValue)
            {
                var tipoObj = await _context.TiposEquipo.FindAsync(tipoId.Value);
                if (EsComponentePcCompleto(tipoObj?.tipo))
                {
                    var pcCompleto = await _context.TiposEquipo
                        .FirstOrDefaultAsync(t => t.tipo != null && t.tipo.ToUpper().Contains("PC COMPLETO"));
                    tipoIdPcCompleto = pcCompleto?.idTipoEquipo;
                }
            }

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
            if (tipoId.HasValue)
            {
                if (tipoIdPcCompleto.HasValue)
                    // Mostrar el componente específico Y PC Completo
                    query = query.Where(a => a.Equipo.idTipoEquipo == tipoId || a.Equipo.idTipoEquipo == tipoIdPcCompleto);
                else
                    query = query.Where(a => a.Equipo.idTipoEquipo == tipoId);
            }
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
            sb.AppendLine("sep=;");
            sb.AppendLine("ID;Empleado;DNI;Tipo Equipo;Equipo;N° Serie;Estado;Fecha Asignación;Grupo/Área;Chip;Correo Equipo");

            foreach (var a in asigs)
            {
                sb.AppendLine($"{a.IdAsignacion};" +
                    $"\"{a.Empleado?.nombre} {a.Empleado?.paterno}\";" +
                    $"\"{a.Empleado?.dni ?? "—"}\";" +
                    $"\"{a.Equipo?.TipoEquipo?.tipo ?? "—"}\";" +
                    $"\"{a.Equipo?.marca} {a.Equipo?.modelo}\";" +
                    $"\"{a.Equipo?.numero_serie ?? "—"}\";" +
                    $"\"{a.EstadoAsignacion}\";" +
                    $"{a.FechaAsignacion:dd/MM/yyyy};" +
                    $"\"{a.Grupo?.area ?? "—"}\";" +
                    $"\"{a.Chip?.NumeroCelular ?? "—"}\";" +
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
                    page.DefaultTextStyle(t => t.FontSize(9));

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
            sb.AppendLine("sep=;");
            sb.AppendLine("Origen;Fecha;Tipo Equipo;Equipo;N° Serie;Evento;Detalle;Valor Anterior;Valor Nuevo;Motivo;Observaciones;Usuario");

            foreach (var l in logs)
            {
                sb.AppendLine($"Componente," +
                    $"{l.FechaHora:dd/MM/yyyy HH:mm};" +
                    $"\"{l.Equipo?.TipoEquipo?.tipo ?? "—"}\";" +
                    $"\"{l.Equipo?.marca} {l.Equipo?.modelo}\";" +
                    $"\"{l.Equipo?.numero_serie ?? "—"}\";" +
                    $"\"{l.TipoEvento}\";" +
                    $"\"{l.Componente ?? "—"}\";" +
                    $"\"{l.ValorAnterior ?? "—"}\";" +
                    $"\"{l.ValorNuevo ?? "—"}\";" +
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
                    page.DefaultTextStyle(t => t.FontSize(9));

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
         // ── INDEX FLOTA ───────────────────────────────────────────
        public async Task<IActionResult> IndexFlota()
        {
            ViewData["Title"]      = "Reportes Flota Vehicular";
            ViewData["Breadcrumb"] = "Reportes / Flota Vehicular";
            ViewBag.Grupos = await _context.Grupos.OrderBy(g => g.area).ToListAsync();
            ViewBag.TiposMantenimiento = await _context.TiposMantenimiento
                .OrderBy(t => t.Nombre).ToListAsync();
            return View();
        }
 
        // ── VEHÍCULOS DATA (AJAX) ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> VehiculosData(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estado, string? buscar)
        {
            var query = _context.Carros
                .Include(c => c.EmpleadosCarros).ThenInclude(ec => ec.Empleado)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(c => c.FechaCompra >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(c => c.FechaCompra <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(c => estados.Contains(c.Estado));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c =>
                    c.Placa.Contains(buscar) || c.Marca.Contains(buscar) || c.Modelo.Contains(buscar) ||
                    (c.NumeroMotor != null && c.NumeroMotor.Contains(buscar)));
 
            var data = await query.OrderByDescending(c => c.IdCarro)
                .Select(c => new {
                    c.IdCarro,
                    c.Placa,
                    c.Marca,
                    c.Modelo,
                    c.Estado,
                    c.Categoria,
                    fechaCompra  = c.FechaCompra != null ? c.FechaCompra.Value.ToString("dd/MM/yyyy") : "—",
                    conductor    = c.EmpleadosCarros.Any()
                        ? c.EmpleadosCarros.First().Empleado.nombre + " " + c.EmpleadosCarros.First().Empleado.paterno
                        : "Sin conductor",
                    c.NumeroMotor,
                }).ToListAsync();
 
            return Json(new { total = data.Count, registros = data });
        }
 
        // ── VEHÍCULOS CSV ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> VehiculosCsv(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estado, string? buscar)
        {
            var query = _context.Carros
                .Include(c => c.EmpleadosCarros).ThenInclude(ec => ec.Empleado)
                .Include(c => c.MantenimientosCarros)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(c => c.FechaCompra >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(c => c.FechaCompra <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(c => estados.Contains(c.Estado));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c => c.Placa.Contains(buscar) || c.Marca.Contains(buscar));
 
            var carros = await query.OrderByDescending(c => c.IdCarro).ToListAsync();
 
            var sb = new StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("ID;Placa;Marca;Modelo;Estado;Categoría;F.Compra;Conductor;N° Motor;N° Mantenimientos");
 
            foreach (var c in carros)
            {
                var conductor = c.EmpleadosCarros.FirstOrDefault();
                var nombreConductor = conductor != null
                    ? $"{conductor.Empleado?.nombre} {conductor.Empleado?.paterno}".Trim()
                    : "Sin conductor";
                var mantes = c.MantenimientosCarros.Count;
 
                sb.AppendLine($"{c.IdCarro};" +
                    $"\"{c.Placa}\";" +
                    $"\"{c.Marca}\";" +
                    $"\"{c.Modelo}\";" +
                    $"\"{c.Estado}\";" +
                    $"\"{c.Categoria ?? "—"}\";" +
                    $"{(c.FechaCompra.HasValue ? c.FechaCompra.Value.ToString("dd/MM/yyyy") : "—")};" +
                    $"\"{nombreConductor}\";" +
                    $"\"{c.NumeroMotor ?? "—"}\";" +
                    $"{mantes}");
            }
 
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Vehiculos_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
 
        // ── VEHÍCULOS PDF ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> VehiculosPdf(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estado, string? buscar)
        {
            var query = _context.Carros
                .Include(c => c.EmpleadosCarros).ThenInclude(ec => ec.Empleado)
                .Include(c => c.MantenimientosCarros)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(c => c.FechaCompra >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(c => c.FechaCompra <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(c => estados.Contains(c.Estado));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c => c.Placa.Contains(buscar) || c.Marca.Contains(buscar));
 
            var carros = await query.OrderByDescending(c => c.IdCarro).ToListAsync();
 
            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(9));
 
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("REPORTE DE FLOTA VEHICULAR")
                                    .Bold().FontSize(14).FontColor(Color.FromHex("#1a3a6b"));
                                c.Item().Text($"SG-JHOMERON — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });
                            row.ConstantItem(140).AlignRight().Column(c =>
                                c.Item().Text($"Total: {carros.Count} vehículo(s)")
                                    .Bold().FontSize(10).FontColor(Color.FromHex("#2563eb")));
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Color.FromHex("#1a3a6b"));
                    });
 
                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(25);
                            c.RelativeColumn(1.5f); // Placa
                            c.RelativeColumn(1.5f); // Marca
                            c.RelativeColumn(1.5f); // Modelo
                            c.RelativeColumn(1.2f); // Estado
                            c.RelativeColumn(1.2f); // Categoría
                            c.RelativeColumn(1.2f); // F.Compra
                            c.RelativeColumn(2.5f); // Conductor
                            c.ConstantColumn(35);   // Mantes
                        });
 
                        static IContainer Cab(IContainer c) =>
                            c.Background(Color.FromHex("#1a3a6b")).Padding(5);
 
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#", "Placa", "Marca", "Modelo", "Estado", "Categoría", "F.Compra", "Conductor", "Mant." })
                                h.Cell().Element(Cab).Text(t).Bold().FontSize(8).FontColor(Colors.White);
                        });
 
                        for (int i = 0; i < carros.Count; i++)
                        {
                            var c  = carros[i];
                            var bg = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                            IContainer C(IContainer cel) => cel.Background(bg).Padding(4);
 
                            var conductor = c.EmpleadosCarros.FirstOrDefault();
                            var nombre    = conductor != null
                                ? $"{conductor.Empleado?.nombre} {conductor.Empleado?.paterno}".Trim()
                                : "Sin conductor";
                            var mantes  = c.MantenimientosCarros.Count;
                            var estadoColor = c.Estado switch {
                                "Activo"           => Color.FromHex("#16a34a"),
                                "En mantenimiento" => Color.FromHex("#d97706"),
                                "Inactivo"         => Color.FromHex("#6b7280"),
                                _                  => Color.FromHex("#dc2626")
                            };
 
                            table.Cell().Element(C).Text($"{i+1}").FontColor(Color.FromHex("#9ca3af"));
                            table.Cell().Element(C).Text(c.Placa).Bold().FontColor(Color.FromHex("#1a3a6b"));
                            table.Cell().Element(C).Text(c.Marca);
                            table.Cell().Element(C).Text(c.Modelo);
                            table.Cell().Element(C).Text(c.Estado).FontColor(estadoColor);
                            table.Cell().Element(C).Text(c.Categoria ?? "—").FontSize(8);
                            table.Cell().Element(C).Text(c.FechaCompra.HasValue ? c.FechaCompra.Value.ToString("dd/MM/yyyy") : "—").FontSize(8);
                            table.Cell().Element(C).Text(nombre).FontColor(Color.FromHex("#2563eb")).FontSize(8);
                            table.Cell().Element(C).AlignCenter()
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
 
            return File(pdf.GeneratePdf(), "application/pdf", $"Vehiculos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
 
        // ── MANTENIMIENTO DATA (AJAX) ─────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MantenimientoData(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estadoMante, int? tipoManteId, string? buscar)
        {
            var query = _context.MantenimientosCarros
                .Include(m => m.Carro)
                .Include(m => m.TipoMantenimiento)
                .Include(m => m.UsuarioCreador)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(m => m.FechaProgramada >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(m => m.FechaProgramada <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estadoMante))
            {
                var estados = estadoMante.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(m => estados.Contains(m.Estado));
            }
            if (tipoManteId.HasValue) query = query.Where(m => m.IdTipoMante == tipoManteId);
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(m =>
                    m.Carro.Placa.Contains(buscar) ||
                    m.Carro.Marca.Contains(buscar)  ||
                    (m.Observaciones != null && m.Observaciones.Contains(buscar)));
 
            var data = await query.OrderByDescending(m => m.FechaProgramada)
                .Select(m => new {
                    m.IdMante,
                    placa        = m.Carro.Placa,
                    vehiculo     = m.Carro.Marca + " " + m.Carro.Modelo,
                    tipo         = m.TipoMantenimiento.Nombre,
                    m.Estado,
                    fechaProg    = m.FechaProgramada.ToString("dd/MM/yyyy"),
                    fechaInicio  = m.FechaInicio != null ? m.FechaInicio.Value.ToString("dd/MM/yyyy") : "—",
                    fechaFin     = m.FechaCulminada != null ? m.FechaCulminada.Value.ToString("dd/MM/yyyy") : "—",
                    obs          = m.Observaciones ?? "—",
                    creador      = m.UsuarioCreador != null ? m.UsuarioCreador.nombreCompleto : "—",
                }).ToListAsync();
 
            return Json(new { total = data.Count, registros = data });
        }
 
        // ── MANTENIMIENTO CSV ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MantenimientoCsv(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estadoMante, int? tipoManteId, string? buscar)
        {
            var query = _context.MantenimientosCarros
                .Include(m => m.Carro)
                .Include(m => m.TipoMantenimiento)
                .Include(m => m.UsuarioCreador)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(m => m.FechaProgramada >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(m => m.FechaProgramada <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estadoMante))
            {
                var estados = estadoMante.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(m => estados.Contains(m.Estado));
            }
            if (tipoManteId.HasValue) query = query.Where(m => m.IdTipoMante == tipoManteId);
 
            var mantes = await query.OrderByDescending(m => m.FechaProgramada).ToListAsync();
 
            var sb = new StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("ID;Placa;Vehículo;Tipo Mantenimiento;Estado;F.Programada;F.Inicio;F.Culminada;Observaciones;Registrado por");
 
            foreach (var m in mantes)
            {
                sb.AppendLine($"{m.IdMante};" +
                    $"\"{m.Carro?.Placa ?? "—"}\";" +
                    $"\"{m.Carro?.Marca} {m.Carro?.Modelo}\";" +
                    $"\"{m.TipoMantenimiento?.Nombre ?? "—"}\";" +
                    $"\"{m.Estado}\";" +
                    $"{m.FechaProgramada:dd/MM/yyyy};" +
                    $"{(m.FechaInicio.HasValue ? m.FechaInicio.Value.ToString("dd/MM/yyyy") : "—")};" +
                    $"{(m.FechaCulminada.HasValue ? m.FechaCulminada.Value.ToString("dd/MM/yyyy") : "—")};" +
                    $"\"{m.Observaciones ?? "—"}\";" +
                    $"\"{m.UsuarioCreador?.nombreCompleto ?? "—"}\"");
            }
 
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Mantenimiento_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
 
        // ── MANTENIMIENTO PDF ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MantenimientoPdf(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estadoMante, int? tipoManteId, string? buscar)
        {
            var query = _context.MantenimientosCarros
                .Include(m => m.Carro)
                .Include(m => m.TipoMantenimiento)
                .Include(m => m.UsuarioCreador)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(m => m.FechaProgramada >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(m => m.FechaProgramada <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estadoMante))
            {
                var estados = estadoMante.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(m => estados.Contains(m.Estado));
            }
            if (tipoManteId.HasValue) query = query.Where(m => m.IdTipoMante == tipoManteId);
 
            var mantes = await query.OrderByDescending(m => m.FechaProgramada).ToListAsync();
 
            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(9));
 
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("REPORTE DE MANTENIMIENTOS VEHICULARES")
                                    .Bold().FontSize(13).FontColor(Color.FromHex("#1a3a6b"));
                                c.Item().Text($"SG-JHOMERON — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });
                            row.ConstantItem(140).AlignRight().Column(c =>
                                c.Item().Text($"Total: {mantes.Count} registro(s)")
                                    .Bold().FontSize(10).FontColor(Color.FromHex("#2563eb")));
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Color.FromHex("#1a3a6b"));
                    });
 
                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(25);
                            c.RelativeColumn(1.2f); // Placa
                            c.RelativeColumn(2f);   // Vehículo
                            c.RelativeColumn(2f);   // Tipo
                            c.RelativeColumn(1.3f); // Estado
                            c.RelativeColumn(1.2f); // F.Prog
                            c.RelativeColumn(1.2f); // F.Inicio
                            c.RelativeColumn(1.2f); // F.Fin
                            c.RelativeColumn(2.5f); // Observaciones
                        });
 
                        static IContainer Cab(IContainer c) =>
                            c.Background(Color.FromHex("#1a3a6b")).Padding(5);
 
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#", "Placa", "Vehículo", "Tipo Mant.", "Estado", "F.Programada", "F.Inicio", "F.Fin", "Observaciones" })
                                h.Cell().Element(Cab).Text(t).Bold().FontSize(8).FontColor(Colors.White);
                        });
 
                        for (int i = 0; i < mantes.Count; i++)
                        {
                            var m  = mantes[i];
                            var bg = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                            IContainer C(IContainer cel) => cel.Background(bg).Padding(4);
 
                            var estadoColor = m.Estado switch {
                                "Culminado"  => Color.FromHex("#16a34a"),
                                "En proceso" => Color.FromHex("#2563eb"),
                                "Pendiente"  => Color.FromHex("#d97706"),
                                "Cancelado"  => Color.FromHex("#dc2626"),
                                _            => Color.FromHex("#6b7280")
                            };
 
                            table.Cell().Element(C).Text($"{i+1}").FontColor(Color.FromHex("#9ca3af"));
                            table.Cell().Element(C).Text(m.Carro?.Placa ?? "—").Bold().FontColor(Color.FromHex("#1a3a6b"));
                            table.Cell().Element(C).Text($"{m.Carro?.Marca} {m.Carro?.Modelo}").FontSize(8);
                            table.Cell().Element(C).Text(m.TipoMantenimiento?.Nombre ?? "—");
                            table.Cell().Element(C).Text(m.Estado).FontColor(estadoColor);
                            table.Cell().Element(C).Text(m.FechaProgramada.ToString("dd/MM/yyyy")).FontSize(8);
                            table.Cell().Element(C).Text(m.FechaInicio.HasValue ? m.FechaInicio.Value.ToString("dd/MM/yyyy") : "—").FontSize(8);
                            table.Cell().Element(C).Text(m.FechaCulminada.HasValue ? m.FechaCulminada.Value.ToString("dd/MM/yyyy") : "—").FontSize(8);
                            table.Cell().Element(C).Text(m.Observaciones ?? "—").FontSize(7.5f).FontColor(Color.FromHex("#4b5563"));
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
 
            return File(pdf.GeneratePdf(), "application/pdf", $"Mantenimiento_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
 
        // ── DASHBOARD FLOTA ───────────────────────────────────────
        public IActionResult DashboardFlota()
        {
            ViewData["Title"]      = "Dashboard Flota Vehicular";
            ViewData["Breadcrumb"] = "Reportes / Dashboard Flota";
            return View();
        }
 
        [HttpGet]
        public async Task<IActionResult> DashboardFlotaData()
        {
            var porEstado = await _context.Carros
                .GroupBy(c => c.Estado)
                .Select(g => new { estado = g.Key, total = g.Count() })
                .ToListAsync();
 
            var porCategoria = await _context.Carros
                .Where(c => c.Categoria != null)
                .GroupBy(c => c.Categoria!)
                .Select(g => new { categoria = g.Key, total = g.Count() })
                .OrderByDescending(g => g.total)
                .ToListAsync();
 
            var hace12Inicio = new DateTime(DateTime.Now.AddMonths(-11).Year, DateTime.Now.AddMonths(-11).Month, 1);
            var mantesPorMes = await _context.MantenimientosCarros
                .Where(m => m.FechaProgramada >= hace12Inicio)
                .GroupBy(m => new { m.FechaProgramada.Year, m.FechaProgramada.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, total = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();
 
            var mesesCompletos = Enumerable.Range(0, 12).Select(i => {
                var d     = hace12Inicio.AddMonths(i);
                var found = mantesPorMes.FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month);
                return new { mes = $"{d:MM}/{d.Year}", total = found?.total ?? 0 };
            }).ToList();
 
            var topMantes = await _context.MantenimientosCarros
                .Include(m => m.Carro)
                .GroupBy(m => new { m.IdCarro, m.Carro.Placa, m.Carro.Marca, m.Carro.Modelo })
                .Select(g => new { g.Key.IdCarro, g.Key.Placa, g.Key.Marca, g.Key.Modelo, total = g.Count() })
                .OrderByDescending(g => g.total)
                .Take(10)
                .ToListAsync();
 
            var totalVehiculos  = await _context.Carros.CountAsync();
            var totalActivos    = await _context.Carros.CountAsync(c => c.Estado == "Activo");
            var totalMante      = await _context.Carros.CountAsync(c => c.Estado == "En mantenimiento");
            var totalPendientes = await _context.MantenimientosCarros.CountAsync(m => m.Estado == "Pendiente");
 
            return Json(new {
                resumen = new { totalVehiculos, totalActivos, totalMante, totalPendientes },
                porEstado,
                porCategoria,
                mantesPorMes = mesesCompletos,
                topMantes
            });
        }
         // ── INDEX PRODUCCIÓN ──────────────────────────────────────
        public async Task<IActionResult> IndexProduccion()
        {
            ViewData["Title"]      = "Reportes Producción";
            ViewData["Breadcrumb"] = "Reportes / Producción";
            ViewBag.Grupos = await _context.Grupos.OrderBy(g => g.area).ToListAsync();
            return View();
        }
 
        // ── MÁQUINAS DATA ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MaquinasData(
            DateTime? fechaDesde, DateTime? fechaHasta,
            string? estado, string? buscar)
        {
            var query = _context.Maquinas
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Grupo : null)
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Encargado : null)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(m => m.FechaCompra >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(m => m.FechaCompra <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(m => estados.Contains(m.Estado));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(m =>
                    m.NumeroMaquina.Contains(buscar) ||
                    m.NombreMaquina.Contains(buscar) ||
                    (m.Marca != null && m.Marca.Contains(buscar)));
 
            var data = await query.OrderByDescending(m => m.IdMaquina)
                .Select(m => new {
                    m.IdMaquina,
                    m.NumeroMaquina,
                    m.NombreMaquina,
                    m.Marca,
                    m.Estado,
                    fechaCompra= m.FechaCompra.HasValue ? m.FechaCompra.Value.ToString("dd/MM/yyyy") : "—",
                    grupo      = m.AsignacionActual != null && m.AsignacionActual.Grupo != null ? m.AsignacionActual.Grupo.area : "Sin asignar",
                    encargado  = m.AsignacionActual != null && m.AsignacionActual.Encargado != null
                        ? m.AsignacionActual.Encargado.nombre + " " + m.AsignacionActual.Encargado.paterno : "—",
                    estadoOp   = m.AsignacionActual != null ? m.AsignacionActual.EstadoOperativo : "—",
                    m.Observaciones,
                }).ToListAsync();
 
            return Json(new { total = data.Count, registros = data });
        }
 
        // ── MÁQUINAS CSV ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MaquinasCsv(
            DateTime? fechaDesde, DateTime? fechaHasta, string? estado, string? buscar)
        {
            var query = _context.Maquinas
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Grupo : null)
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Encargado : null)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(m => m.FechaCompra >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(m => m.FechaCompra <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(m => estados.Contains(m.Estado));
            }
 
            var maquinas = await query.OrderByDescending(m => m.IdMaquina).ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("N° Máquina;Nombre;Marca;Estado;F.Compra;Grupo Asignado;Encargado;Estado Operativo;Observaciones");
 
            foreach (var m in maquinas)
            {
                var asig = m.AsignacionActual;
                sb.AppendLine($"\"{m.NumeroMaquina}\";" +
                    $"\"{m.NombreMaquina}\";" +
                    $"\"{m.Marca ?? "—"}\";" +
                    $"\"{m.Estado}\";" +
                    $"{(m.FechaCompra.HasValue ? m.FechaCompra.Value.ToString("dd/MM/yyyy") : "—")};" +
                    $"\"{asig?.Grupo?.area ?? "Sin asignar"}\";" +
                    $"\"{(asig?.Encargado != null ? asig.Encargado.nombre + " " + asig.Encargado.paterno : "—")}\";" +
                    $"\"{asig?.EstadoOperativo ?? "—"}\";" +
                    $"\"{m.Observaciones ?? "—"}\"");
            }
 
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Maquinas_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
 
        // ── MÁQUINAS PDF ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MaquinasPdf(
            DateTime? fechaDesde, DateTime? fechaHasta, string? estado, string? buscar)
        {
            var query = _context.Maquinas
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Grupo : null)
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Encargado : null)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(m => m.FechaCompra >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(m => m.FechaCompra <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estados = estado.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(m => estados.Contains(m.Estado));
            }
 
            var maquinas = await query.OrderByDescending(m => m.IdMaquina).ToListAsync();
 
            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(9));
 
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("REPORTE DE MÁQUINAS — PRODUCCIÓN")
                                    .Bold().FontSize(13).FontColor(Color.FromHex("#1a3a6b"));
                                c.Item().Text($"SG-JHOMERON — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });
                            row.ConstantItem(140).AlignRight().Column(c =>
                                c.Item().Text($"Total: {maquinas.Count} máquina(s)")
                                    .Bold().FontSize(10).FontColor(Color.FromHex("#2563eb")));
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Color.FromHex("#1a3a6b"));
                    });
 
                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(25);
                            c.RelativeColumn(1.3f); // N° Maq
                            c.RelativeColumn(2.5f); // Nombre
                            c.RelativeColumn(1.2f); // Marca
                            c.RelativeColumn(1.2f); // Estado
                            c.RelativeColumn(1.2f); // F.Compra
                            c.RelativeColumn(2f);   // Grupo
                            c.RelativeColumn(2f);   // Encargado
                            c.RelativeColumn(1.2f); // Est.Op
                        });
 
                        static IContainer Cab(IContainer c) =>
                            c.Background(Color.FromHex("#1a3a6b")).Padding(5);
 
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#","N° Máq.","Nombre","Marca","Estado","F.Compra","Grupo","Encargado","Est.Op." })
                                h.Cell().Element(Cab).Text(t).Bold().FontSize(8).FontColor(Colors.White);
                        });
 
                        for (int i = 0; i < maquinas.Count; i++)
                        {
                            var m  = maquinas[i];
                            var bg = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                            IContainer C(IContainer c) => c.Background(bg).Padding(4);
                            var asig = m.AsignacionActual;
 
                            var estadoColor = m.Estado switch {
                                "Activo"        => Color.FromHex("#16a34a"),
                                "Mantenimiento" => Color.FromHex("#d97706"),
                                "Inoperativo"   => Color.FromHex("#dc2626"),
                                _               => Color.FromHex("#6b7280")
                            };
 
                            table.Cell().Element(C).Text($"{i+1}").FontColor(Color.FromHex("#9ca3af"));
                            table.Cell().Element(C).Text(m.NumeroMaquina).Bold().FontColor(Color.FromHex("#1a3a6b")).FontSize(8);
                            table.Cell().Element(C).Text(m.NombreMaquina).Bold();
                            table.Cell().Element(C).Text(m.Marca ?? "—").FontSize(8);
                            table.Cell().Element(C).Text(m.Estado).FontColor(estadoColor);
                            table.Cell().Element(C).Text(m.FechaCompra.HasValue ? m.FechaCompra.Value.ToString("dd/MM/yy") : "—").FontSize(8);
                            table.Cell().Element(C).Text(asig?.Grupo?.area ?? "Sin asignar").FontSize(8);
                            table.Cell().Element(C).Text(asig?.Encargado != null ? $"{asig.Encargado.nombre} {asig.Encargado.paterno}" : "—").FontSize(8).FontColor(Color.FromHex("#2563eb"));
                            table.Cell().Element(C).Text(asig?.EstadoOperativo ?? "—").FontSize(8)
                                .FontColor(asig?.EstadoOperativo == "Operativo" ? Color.FromHex("#16a34a") : Color.FromHex("#dc2626"));
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
 
            return File(pdf.GeneratePdf(), "application/pdf", $"Maquinas_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
 
        // ── HISTORIAL MÁQUINAS DATA ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> HistorialMaquinasData(
            DateTime? fechaDesde, DateTime? fechaHasta, string? tipoEvento, string? buscar)
        {
            var query = _context.MaquinaLogs
                .Include(l => l.Maquina)
                .AsQueryable();
 
            if (fechaDesde.HasValue) query = query.Where(l => l.FechaHora >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(l => l.FechaHora <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(tipoEvento))
            {
                var tipos = tipoEvento.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(l => tipos.Contains(l.TipoEvento));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(l =>
                    l.Maquina.NumeroMaquina.Contains(buscar) ||
                    l.Maquina.NombreMaquina.Contains(buscar));
 
            var data = await query.OrderByDescending(l => l.FechaHora)
                .Select(l => new {
                    l.IdLog,
                    idMaquina   = l.IdMaquina,
                    numMaquina  = l.Maquina.NumeroMaquina,
                    nomMaquina  = l.Maquina.NombreMaquina,
                    l.TipoEvento,
                    l.ValorAnterior,
                    l.ValorNuevo,
                    l.Observaciones,
                    l.NombreUsuario,
                    fecha = l.FechaHora.ToString("dd/MM/yyyy HH:mm")
                }).ToListAsync();
 
            return Json(new { total = data.Count, registros = data });
        }
 
        // ── HISTORIAL MÁQUINAS CSV ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> HistorialMaquinasCsv(
            DateTime? fechaDesde, DateTime? fechaHasta, string? tipoEvento, string? buscar)
        {
            var query = _context.MaquinaLogs.Include(l => l.Maquina).AsQueryable();
            if (fechaDesde.HasValue) query = query.Where(l => l.FechaHora >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(l => l.FechaHora <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(tipoEvento))
            {
                var tipos = tipoEvento.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(l => tipos.Contains(l.TipoEvento));
            }
 
            var logs = await query.OrderByDescending(l => l.FechaHora).ToListAsync();
            var sb   = new StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("Fecha;N° Máquina;Nombre;Tipo Evento;Anterior;Nuevo;Observaciones;Usuario");
 
            foreach (var l in logs)
            {
                sb.AppendLine($"{l.FechaHora:dd/MM/yyyy HH:mm};" +
                    $"\"{l.Maquina?.NumeroMaquina ?? "—"}\";" +
                    $"\"{l.Maquina?.NombreMaquina ?? "—"}\";" +
                    $"\"{l.TipoEvento}\";" +
                    $"\"{l.ValorAnterior ?? "—"}\";" +
                    $"\"{l.ValorNuevo ?? "—"}\";" +
                    $"\"{l.Observaciones ?? "—"}\";" +
                    $"\"{l.NombreUsuario ?? "—"}\"");
            }
 
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"HistorialMaquinas_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
 
        // ── DASHBOARD PRODUCCIÓN ──────────────────────────────────
        public IActionResult DashboardProduccion()
        {
            ViewData["Title"]      = "Dashboard Producción";
            ViewData["Breadcrumb"] = "Reportes / Dashboard Producción";
            return View();
        }
 
        [HttpGet]
        public async Task<IActionResult> DashboardProduccionData()
        {
            var porEstado = await _context.Maquinas
                .GroupBy(m => m.Estado)
                .Select(g => new { estado = g.Key, total = g.Count() })
                .ToListAsync();
 
            var porGrupo = await _context.MaquinaAsignaciones
                .Include(a => a.Grupo)
                .Where(a => a.EsActiva)
                .GroupBy(a => a.Grupo != null ? a.Grupo.area : "Sin área")
                .Select(g => new { grupo = g.Key, total = g.Count() })
                .OrderByDescending(g => g.total)
                .ToListAsync();
 
            var hace12Inicio = new DateTime(DateTime.Now.AddMonths(-11).Year, DateTime.Now.AddMonths(-11).Month, 1);
            var logsPorMes = await _context.MaquinaLogs
                .Where(l => l.TipoEvento == "CambioEstado" && l.FechaHora >= hace12Inicio)
                .GroupBy(l => new { l.FechaHora.Year, l.FechaHora.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, total = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();
 
            var mesesCompletos = Enumerable.Range(0, 12).Select(i => {
                var d     = hace12Inicio.AddMonths(i);
                var found = logsPorMes.FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month);
                return new { mes = $"{d:MM}/{d.Year}", total = found?.total ?? 0 };
            }).ToList();
 
            var totalMaquinas  = await _context.Maquinas.CountAsync();
            var totalActivas   = await _context.Maquinas.CountAsync(m => m.Estado == "Activo");
            var totalMante     = await _context.Maquinas.CountAsync(m => m.Estado == "Mantenimiento");
            var totalAsignadas = await _context.MaquinaAsignaciones.CountAsync(a => a.EsActiva);
 
            return Json(new {
                resumen = new { totalMaquinas, totalActivas, totalMante, totalAsignadas },
                porEstado,
                porGrupo,
                logsPorMes = mesesCompletos
            });
        }
 
        // ── HISTORIAL MÁQUINAS PDF ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> HistorialMaquinasPdf(
            DateTime? fechaDesde, DateTime? fechaHasta, string? tipoEvento, string? buscar)
        {
            var query = _context.MaquinaLogs.Include(l => l.Maquina).AsQueryable();
            if (fechaDesde.HasValue) query = query.Where(l => l.FechaHora >= fechaDesde);
            if (fechaHasta.HasValue) query = query.Where(l => l.FechaHora <= fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(tipoEvento))
            {
                var tipos = tipoEvento.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(l => tipos.Contains(l.TipoEvento));
            }
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(l =>
                    l.Maquina.NumeroMaquina.Contains(buscar) ||
                    l.Maquina.NombreMaquina.Contains(buscar));
 
            var logs = await query.OrderByDescending(l => l.FechaHora).ToListAsync();
 
            var pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(t => t.FontSize(8.5f));
 
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("HISTORIAL DE MÁQUINAS — PRODUCCIÓN")
                                    .Bold().FontSize(13).FontColor(Color.FromHex("#1a3a6b"));
                                c.Item().Text($"SG-JHOMERON — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Color.FromHex("#6b7280"));
                            });
                            row.ConstantItem(140).AlignRight().Column(c =>
                                c.Item().Text($"Total: {logs.Count} registro(s)")
                                    .Bold().FontSize(10).FontColor(Color.FromHex("#2563eb")));
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Color.FromHex("#1a3a6b"));
                    });
 
                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.5f); // Fecha
                            c.RelativeColumn(1.2f); // N° Maq
                            c.RelativeColumn(2f);   // Nombre
                            c.RelativeColumn(1.5f); // Evento
                            c.RelativeColumn(2f);   // Anterior
                            c.RelativeColumn(2f);   // Nuevo
                            c.RelativeColumn(2f);   // Observación
                            c.RelativeColumn(1.2f); // Usuario
                        });
 
                        static IContainer Cab(IContainer c) =>
                            c.Background(Color.FromHex("#1a3a6b")).Padding(5);
 
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "Fecha/Hora","N° Maq.","Nombre","Evento","Anterior","Nuevo","Observación","Usuario" })
                                h.Cell().Element(Cab).Text(t).Bold().FontSize(7.5f).FontColor(Colors.White);
                        });
 
                        for (int i = 0; i < logs.Count; i++)
                        {
                            var l  = logs[i];
                            var bg = i % 2 == 0 ? Color.FromHex("#f8fafc") : Colors.White;
                            IContainer C(IContainer c) => c.Background(bg).Padding(3);
 
                            var evColor = l.TipoEvento switch {
                                "CambioEstado"     => Color.FromHex("#d97706"),
                                "CambioAsignacion" => Color.FromHex("#2563eb"),
                                "CambioEncargado"  => Color.FromHex("#0891b2"),
                                "Edicion"          => Color.FromHex("#6b7280"),
                                _                  => Color.FromHex("#6b7280")
                            };
 
                            table.Cell().Element(C).Text(l.FechaHora.ToString("dd/MM/yy HH:mm")).FontSize(7.5f).FontColor(Color.FromHex("#6b7280"));
                            table.Cell().Element(C).Text(l.Maquina?.NumeroMaquina ?? "—").Bold().FontColor(Color.FromHex("#1a3a6b")).FontSize(7.5f);
                            table.Cell().Element(C).Text(l.Maquina?.NombreMaquina ?? "—").FontSize(8);
                            table.Cell().Element(C).Text(l.TipoEvento).FontColor(evColor).FontSize(7.5f);
                            table.Cell().Element(C).Text(l.ValorAnterior ?? "—").FontSize(7.5f).FontColor(Color.FromHex("#6b7280"));
                            table.Cell().Element(C).Text(l.ValorNuevo ?? "—").FontSize(7.5f).FontColor(Color.FromHex("#2563eb"));
                            table.Cell().Element(C).Text(l.Observaciones ?? "—").FontSize(7f).FontColor(Color.FromHex("#4b5563"));
                            table.Cell().Element(C).Text(l.NombreUsuario ?? "—").FontSize(7f).FontColor(Color.FromHex("#9ca3af"));
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
 
            return File(pdf.GeneratePdf(), "application/pdf", $"HistorialMaquinas_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json;

namespace PROYJHOME2026.Controllers
{
    public class IAController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAService    _iaService;

        public IAController(AppDbContext context, IAService iaService)
        {
            _context   = context;
            _iaService = iaService;
        }

        private async Task<Usuario?> ObtenerUsuarioAsync()
        {
            var idStr = HttpContext.Session.GetString("UsuarioId");
            if (!int.TryParse(idStr, out int id)) return null;
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.idUsuario == id);
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null) return RedirectToAction("Login", "Auth");

            var convActiva = await _context.IAConversaciones
                .Include(c => c.Mensajes)
                .FirstOrDefaultAsync(c => c.IdUsuario == usuario.idUsuario && c.EsActiva);

            if (convActiva == null)
                convActiva = await _iaService.NuevaConversacionAsync(usuario.idUsuario);

            var historial = await _context.IAConversaciones
                .Where(c => c.IdUsuario == usuario.idUsuario && !c.EsActiva)
                .OrderByDescending(c => c.FechaUltimoMensaje)
                .Take(5)
                .ToListAsync();

            var dashboard = await _iaService.ObtenerDashboardAsync(usuario);

            var mensajes = await _context.IAMensajes
                .Where(m => m.IdConversacion == convActiva.IdConversacion)
                .OrderBy(m => m.FechaCreacion)
                .ToListAsync();

            ViewBag.Usuario    = usuario;
            ViewBag.ConvActiva = convActiva;
            ViewBag.Historial  = historial;
            ViewBag.Dashboard  = dashboard;
            ViewBag.Mensajes   = mensajes;

            return View();
        }

        // ── ENVIAR MENSAJE ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Mensaje([FromBody] MensajeRequest req)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null) return Unauthorized();
            try
            {
                var resultado = await _iaService.EnviarMensajeAsync(
                    req.IdConversacion, req.Mensaje, usuario);
                return Json(new { ok = true, data = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // ── NUEVA CONVERSACIÓN ───────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> NuevaConversacion()
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null) return Unauthorized();
            var conv = await _iaService.NuevaConversacionAsync(usuario.idUsuario);
            return Json(new { ok = true, idConversacion = conv.IdConversacion });
        }

        // ── CARGAR CONVERSACIÓN ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> CargarConversacion(int id)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null) return Unauthorized();

            var conv = await _context.IAConversaciones
                .FirstOrDefaultAsync(c => c.IdConversacion == id && c.IdUsuario == usuario.idUsuario);
            if (conv == null) return NotFound();

            var mensajes = await _context.IAMensajes
                .Where(m => m.IdConversacion == id)
                .OrderBy(m => m.FechaCreacion)
                .Select(m => new {
                    m.Rol, m.Contenido, m.GraficoJson,
                    m.Recomendacion, m.TieneExportacion, m.DatosExportacionJson
                })
                .ToListAsync();

            return Json(new { ok = true, mensajes, titulo = conv.Titulo });
        }

        // ── DASHBOARD AJAX ───────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null) return Unauthorized();
            var dashboard = await _iaService.ObtenerDashboardAsync(usuario);
            return Json(dashboard);
        }

        // ── EXPORTAR EXCEL (CSV) ─────────────────────────────────
        [HttpPost]
        public IActionResult ExportarExcel([FromBody] ExportRequest req)
        {
            if (req?.Columnas == null || req.Filas == null)
                return BadRequest("Sin datos para exportar.");

            var sb = new StringBuilder();
            sb.AppendLine("sep=;");

            // Cabecera
            var cabecera = string.Join(";", req.Columnas.Select(c => "\"" + c + "\""));
            sb.AppendLine(cabecera);

            // Filas
            foreach (var fila in req.Filas)
            {
                var linea = string.Join(";", fila.Select(v =>
                    "\"" + (v ?? "—").Replace("\"", "'") + "\""));
                sb.AppendLine(linea);
            }

            // BOM UTF-8 para que Excel abra correctamente los caracteres especiales
            var bom    = new byte[] { 0xEF, 0xBB, 0xBF };
            var datos  = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes  = bom.Concat(datos).ToArray();
            var nombre = (req.Titulo ?? "Reporte") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";
            return File(bytes, "text/csv; charset=utf-8-sig", nombre);
        }

        // ── EXPORTAR PDF ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ExportarPdf([FromBody] ExportRequest req)
        {
            var usuario = await ObtenerUsuarioAsync();
            if (usuario == null) return Unauthorized();
            if (req?.Columnas == null || req.Filas == null)
                return BadRequest("Sin datos.");

            var pdfBytes = GenerarPdf(req.Titulo ?? "Reporte IA", req.Columnas, req.Filas, usuario);
            var nombre   = (req.Titulo ?? "Reporte") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf";
            return File(pdfBytes, "application/pdf", nombre);
        }

        // ── GENERADOR PDF ─────────────────────────────────────────
        private static byte[] GenerarPdf(string titulo, List<string> columnas,
            List<List<string>> filas, Usuario usuario)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.MarginHorizontal(30);
                    page.MarginVertical(25);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("JHOMERON S.A")
                                    .Bold().FontSize(16).FontColor("#1e3a5f");
                                c.Item().Text(titulo)
                                    .FontSize(12).FontColor("#374151");
                                c.Item().Text(
                                    "Generado por IA - " +
                                    (usuario.nombreCompleto ?? usuario.username) +
                                    " - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                    .FontSize(8).FontColor("#9ca3af");
                            });
                        });
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e5e7eb");
                    });

                    page.Content().PaddingTop(16).Table(table =>
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
                                    .Padding(6)
                                    .Text(col)
                                    .Bold().FontColor("#ffffff").FontSize(9);
                        });

                        var alt = false;
                        foreach (var fila in filas)
                        {
                            var bg = alt ? "#f9fafb" : "#ffffff";
                            foreach (var celda in fila)
                                table.Cell()
                                    .Background(bg)
                                    .BorderBottom(1).BorderColor("#f3f4f6")
                                    .Padding(5)
                                    .Text(celda ?? "—")
                                    .FontSize(8);
                            alt = !alt;
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Página ").FontSize(8).FontColor("#9ca3af");
                        t.CurrentPageNumber().FontSize(8).FontColor("#9ca3af");
                        t.Span(" de ").FontSize(8).FontColor("#9ca3af");
                        t.TotalPages().FontSize(8).FontColor("#9ca3af");
                        t.Span(" - Sistema Jhomeron S.A - " + DateTime.Now.Year.ToString())
                            .FontSize(8).FontColor("#9ca3af");
                    });
                });
            }).GeneratePdf();
        }
    }

    // ── REQUEST MODELS ────────────────────────────────────────────
    public class MensajeRequest
    {
        public int    IdConversacion { get; set; }
        public string Mensaje        { get; set; } = "";
    }

    public class ExportRequest
    {
        public string?             Titulo   { get; set; }
        public List<string>?       Columnas { get; set; }
        public List<List<string>>? Filas    { get; set; }
    }
}
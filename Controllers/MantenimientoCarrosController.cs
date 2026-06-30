using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PROYJHOME2026.Controllers
{
    public class MantenimientoCarrosController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly NotificacionService _notifService;
        private readonly AuditoriaService    _auditoriaService;
        private readonly EmailService        _emailService;
        private readonly TwilioService       _twilioService;

        public MantenimientoCarrosController(
            AppDbContext        context,
            NotificacionService notifService,
            AuditoriaService    auditoriaService,
            EmailService        emailService,
            TwilioService       twilioService)
        {
            _context          = context;
            _notifService     = notifService;
            _auditoriaService = auditoriaService;
            _emailService     = emailService;
            _twilioService    = twilioService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estadoFiltro, string? orden = "desc", int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.MantenimientosCarros
                .Include(m => m.Carro)
                .Include(m => m.TipoMantenimiento)
                .Include(m => m.UsuarioCreador)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(m =>
                    m.Carro.Placa.Contains(buscar)  ||
                    m.Carro.Marca.Contains(buscar)  ||
                    m.TipoMantenimiento.Nombre.Contains(buscar));

            if (!string.IsNullOrWhiteSpace(estadoFiltro))
                query = query.Where(m => m.Estado == estadoFiltro);

            int total = await query.CountAsync();

            var mantenimientos = await query
                .OrderByDescending(m => m.IdMante)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.EstadoFiltro = estadoFiltro;
            ViewBag.Orden        = orden;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(mantenimientos);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro)
                .Include(x => x.TipoMantenimiento)
                .Include(x => x.UsuarioCreador)
                .FirstOrDefaultAsync(x => x.IdMante == id);

            if (m == null) return NotFound();
            return View(m);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public async Task<IActionResult> Create(int? idCarro)
        {
            await CargarListas(idCarro);
            var vm = new MantenimientoCarro
            {
                FechaRegistro    = DateTime.Now,
                FechaProgramada  = DateTime.Today.AddDays(1),
                Estado           = "Pendiente",
                IdCarro          = idCarro ?? 0
            };
            return View(vm);
        }

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MantenimientoCarro vm)
        {
            ModelState.Remove("Carro");
            ModelState.Remove("TipoMantenimiento");
            ModelState.Remove("Estado");
            ModelState.Remove("UsuarioCreador");
            ModelState.Remove("FechaRegistro");

            vm.Estado        = "Pendiente";
            vm.FechaRegistro = DateTime.Now;

            // Asignar usuario creador desde la sesión
            var idStr = HttpContext.Session.GetString("UsuarioId");
            if (int.TryParse(idStr, out int idUsuario))
                vm.IdUsuarioCreador = idUsuario;

            if (ModelState.IsValid)
            {
                _context.Add(vm);
                await _context.SaveChangesAsync();

                // WhatsApp instantaneo a Ayde y Silvana al registrar mantenimiento
                try
                {
                    var carroWsp = await _context.Carros.FindAsync(vm.IdCarro);
                    var tipoWsp  = await _context.TiposMantenimiento.FindAsync(vm.IdTipoMante);
                    var dias     = (vm.FechaProgramada.Date - DateTime.Today).Days;
                    var sufijo   = dias == 0 ? " — HOY" : "";
                    var detDias  = dias < 0 ? " (fecha pasada)" : dias > 0 ? $" (en {dias} dia(s))" : "";
                    var txtWsp   = $"\u2705 *Mantenimiento registrado{sufijo}*\nVehiculo: {carroWsp?.Placa}\nTipo: {tipoWsp?.Nombre}\nFecha programada: {vm.FechaProgramada:dd/MM/yyyy}{detDias}";
                    await _twilioService.EnviarATodosAsync($"mante_nuevo_{vm.IdMante}", txtWsp);
                }
                catch (Exception ex)
                {
                    // No bloquear el flujo si falla WhatsApp
                    _ = ex;
                }

                // Notificación de creación
                 await _notifService.CrearAsync(
                    tipo:    "Creacion",
                    titulo:  $"Nuevo mantenimiento registrado — {vm.Carro?.Placa ?? "vehículo"}",
                    mensaje: $"Se programó un mantenimiento para el {vm.FechaProgramada:dd/MM/yyyy}.",
                    url:     $"/MantenimientoCarros/Details/{vm.IdMante}",
                    idMante: vm.IdMante,
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _mc1) ? _mc1 : null
                );

                // Auditoría
                await _auditoriaService.RegistrarAsync(
                    accion:      "Crear",
                    entidad:     "MantenimientoCarro",
                    idEntidad:   vm.IdMante,
                    descripcion: $"Registró mantenimiento #{vm.IdMante} para vehículo IdCarro={vm.IdCarro} programado el {vm.FechaProgramada:dd/MM/yyyy}"
                );


                // Email al registrar — usuario que actúa + admins
                {
                    var carro = await _context.Carros.FindAsync(vm.IdCarro);
                    var tipo  = await _context.TiposMantenimiento.FindAsync(vm.IdTipoMante);
                    foreach (var u in await ObtenerDestinatariosEmailAsync(vm.IdUsuarioCreador))
                        await _emailService.EnviarAlertaEstadoMantenimientoAsync(
                            u.correo!, u.nombreCompleto ?? u.username,
                            carro?.Placa ?? "—", tipo?.Nombre ?? "—",
                            "Pendiente", vm.FechaProgramada);
                }

                TempData["Success"] = "Mantenimiento registrado. Estado: Pendiente.";
                return RedirectToAction(nameof(Details), new { id = vm.IdMante });
            }

            await CargarListas(vm.IdCarro);
            return View(vm);
        }

        // ── PROCEDER (Pendiente → En proceso) ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Proceder(int id)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro)
                .Include(x => x.TipoMantenimiento)
                .FirstOrDefaultAsync(x => x.IdMante == id);

            if (m == null) return NotFound();
            if (m.Estado != "Pendiente")
            {
                TempData["Warning"] = "Solo se puede proceder desde estado Pendiente.";
                return RedirectToAction(nameof(Details), new { id });
            }

            m.Estado      = "En proceso";
            m.FechaInicio = DateTime.Now;

            // Marcar carro en mantenimiento
            var carro = await _context.Carros.FindAsync(m.IdCarro);
            if (carro != null) carro.Estado = "En mantenimiento";

            await _context.SaveChangesAsync();

            await _notifService.CrearAsync(
                tipo:    "CambioEstado",
                titulo:  $"Mantenimiento en proceso — {m.Carro?.Placa}",
                mensaje: $"El mantenimiento de {m.TipoMantenimiento?.Nombre} ya está en proceso.",
                url:     $"/MantenimientoCarros/Details/{id}",
                idMante: id,
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _mc2) ? _mc2 : null
            );

            await _auditoriaService.RegistrarAsync(
                accion:      "CambioEstado",
                entidad:     "MantenimientoCarro",
                idEntidad:   id,
                descripcion: $"Cambió mantenimiento #{id} de Pendiente → En proceso"
            );

            // WhatsApp + Email — inicio de proceso
            try
            {
                var txtWsp = $"\uD83D\uDD27 *Mantenimiento EN PROCESO*\nVehiculo: {m.Carro?.Placa}\nTipo: {m.TipoMantenimiento?.Nombre}\nInicio: {DateTime.Now:dd/MM/yyyy HH:mm}";
                await _twilioService.EnviarATodosAsync($"mante_proceso_{id}", txtWsp);

                var idUsuarioActivo = int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _uid2) ? _uid2 : (int?)null;
                foreach (var u in await ObtenerDestinatariosEmailAsync(idUsuarioActivo))
                    await _emailService.EnviarAlertaEstadoMantenimientoAsync(
                        u.correo!, u.nombreCompleto ?? u.username,
                        m.Carro?.Placa ?? "—", m.TipoMantenimiento?.Nombre ?? "—",
                        "En proceso", m.FechaProgramada);
            }
            catch (Exception ex) { _ = ex; }

            TempData["Success"] = "Mantenimiento marcado como En proceso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── CULMINAR (En proceso → Culminado) ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Culminar(int id, string? comentarioCulminacion)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro)
                .Include(x => x.TipoMantenimiento)
                .FirstOrDefaultAsync(x => x.IdMante == id);

            if (m == null) return NotFound();
            if (m.Estado != "En proceso")
            {
                TempData["Warning"] = "Solo se puede culminar desde estado En proceso.";
                return RedirectToAction(nameof(Details), new { id });
            }

            m.Estado                 = "Culminado";
            m.FechaCulminada         = DateTime.Now;
            m.ComentarioCulminacion  = comentarioCulminacion;

            // Devolver carro a Activo si no tiene otros mantenimientos en proceso
            bool otrosEnProceso = await _context.MantenimientosCarros
                .AnyAsync(x => x.IdCarro == m.IdCarro && x.Estado == "En proceso" && x.IdMante != id);

            if (!otrosEnProceso)
            {
                var carro = await _context.Carros.FindAsync(m.IdCarro);
                if (carro != null) carro.Estado = "Activo";
            }

            await _context.SaveChangesAsync();

            var descComentario = !string.IsNullOrWhiteSpace(comentarioCulminacion)
                ? $" — {comentarioCulminacion}"
                : "";

            await _notifService.CrearAsync(
                tipo:    "CambioEstado",
                titulo:  $"Mantenimiento culminado — {m.Carro?.Placa}",
                mensaje: $"El mantenimiento de {m.TipoMantenimiento?.Nombre} fue culminado{descComentario}.",
                url:     $"/MantenimientoCarros/Details/{id}",
                idMante: id,
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _mc3) ? _mc3 : null
            );

            await _auditoriaService.RegistrarAsync(
                accion:      "CambioEstado",
                entidad:     "MantenimientoCarro",
                idEntidad:   id,
                descripcion: $"Culminó mantenimiento #{id} — {m.TipoMantenimiento?.Nombre} ({m.Carro?.Placa}){descComentario}"
            );

            // WhatsApp instantaneo — culminacion con comentario
            try
            {
                var comentarioWsp = !string.IsNullOrWhiteSpace(comentarioCulminacion)
                    ? $"\nComentario: {comentarioCulminacion}"
                    : "";
                var txtWsp = $"\uD83C\uDFC1 *Mantenimiento CULMINADO*\nVehiculo: {m.Carro?.Placa}\nTipo: {m.TipoMantenimiento?.Nombre}\nCulminado: {DateTime.Now:dd/MM/yyyy HH:mm}{comentarioWsp}";
                await _twilioService.EnviarATodosAsync($"mante_culminado_{id}", txtWsp);

                // Email al culminar
                var idUsuarioActivo = int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _uid3) ? _uid3 : (int?)null;
                foreach (var u in await ObtenerDestinatariosEmailAsync(idUsuarioActivo))
                    await _emailService.EnviarAlertaEstadoMantenimientoAsync(
                        u.correo!, u.nombreCompleto ?? u.username,
                        m.Carro?.Placa ?? "—", m.TipoMantenimiento?.Nombre ?? "—",
                        "Culminado", m.FechaProgramada);
            }
            catch (Exception ex) { _ = ex; }

            TempData["Success"] = "Mantenimiento culminado. El vehículo volvió a Activo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── CANCELAR ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro)
                .FirstOrDefaultAsync(x => x.IdMante == id);

            if (m == null) return NotFound();

            var estadoAnterior = m.Estado;
            m.Estado = "Cancelado";

            bool otrosEnProceso = await _context.MantenimientosCarros
                .AnyAsync(x => x.IdCarro == m.IdCarro && x.Estado == "En proceso" && x.IdMante != id);

            if (!otrosEnProceso)
            {
                var carro = await _context.Carros.FindAsync(m.IdCarro);
                if (carro != null) carro.Estado = "Activo";
            }

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync(
                accion:      "CambioEstado",
                entidad:     "MantenimientoCarro",
                idEntidad:   id,
                descripcion: $"Canceló mantenimiento #{id} (era {estadoAnterior})"
            );

            TempData["Warning"] = "Mantenimiento cancelado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _context.MantenimientosCarros.FirstOrDefaultAsync(x => x.IdMante == id);
            if (m == null) return NotFound();

            if (m.Estado != "Pendiente")
            {
                TempData["Warning"] = "Solo se pueden editar mantenimientos en estado Pendiente.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await CargarListas(m.IdCarro, m.IdTipoMante);
            return View(m);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MantenimientoCarro vm)
        {
            if (id != vm.IdMante) return NotFound();

            ModelState.Remove("Carro");
            ModelState.Remove("TipoMantenimiento");
            ModelState.Remove("UsuarioCreador");
            ModelState.Remove("FechaRegistro");

            if (ModelState.IsValid)
            {
                var existing = await _context.MantenimientosCarros.FindAsync(id);
                if (existing == null) return NotFound();

                var anterior = $"Tipo={existing.IdTipoMante}, FechaProgramada={existing.FechaProgramada:dd/MM/yyyy}, Obs={existing.Observaciones}";

                existing.IdTipoMante    = vm.IdTipoMante;
                existing.FechaProgramada = vm.FechaProgramada;
                existing.Observaciones  = vm.Observaciones;

                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync(
                    accion:          "Editar",
                    entidad:         "MantenimientoCarro",
                    idEntidad:       id,
                    descripcion:     $"Editó mantenimiento #{id}",
                    datosAnteriores: anterior
                );

                TempData["Success"] = "Mantenimiento actualizado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await CargarListas(vm.IdCarro, vm.IdTipoMante);
            return View(vm);
        }

        // ── DELETE ───────────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro).Include(x => x.TipoMantenimiento)
                .FirstOrDefaultAsync(x => x.IdMante == id);
            if (m == null) return NotFound();
            return View(m);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro)
                .FirstOrDefaultAsync(x => x.IdMante == id);
            if (m == null) return NotFound();

            bool eraEnProceso = m.Estado == "En proceso";
            int  idCarro      = m.IdCarro;
            var  desc         = $"Eliminó mantenimiento #{id} ({m.Carro?.Placa}, estado={m.Estado})";

            _context.MantenimientosCarros.Remove(m);
            await _context.SaveChangesAsync();

            if (eraEnProceso)
            {
                bool otrosEnProceso = await _context.MantenimientosCarros
                    .AnyAsync(x => x.IdCarro == idCarro && x.Estado == "En proceso");
                if (!otrosEnProceso)
                {
                    var carro = await _context.Carros.FindAsync(idCarro);
                    if (carro != null) { carro.Estado = "Activo"; await _context.SaveChangesAsync(); }
                }
            }

            await _auditoriaService.RegistrarAsync("Eliminar", "MantenimientoCarro", id, desc);

            TempData["Success"] = "Mantenimiento eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPER ───────────────────────────────────────────────
        private async Task CargarListas(int? idCarroSel = null, int? idTipoSel = null)
        {
            var carros = await _context.Carros
                .OrderBy(c => c.Placa)
                .Select(c => new { c.IdCarro, Desc = c.Placa + " — " + c.Marca + " " + c.Modelo })
                .ToListAsync();

            var tipos = await _context.TiposMantenimiento.OrderBy(t => t.Nombre).ToListAsync();

            ViewBag.CarrosList = new SelectList(carros,  "IdCarro",     "Desc",   idCarroSel);
            ViewBag.TiposList  = new SelectList(tipos,   "IdTipoMante", "Nombre", idTipoSel);
        }

        /// <summary>
        /// Devuelve destinatarios de correo: el usuario que hizo la acción + todos los admins.
        /// Sin duplicados. Solo usuarios con correo registrado.
        /// </summary>
        private async Task<List<Usuario>> ObtenerDestinatariosEmailAsync(int? idUsuarioAccion)
        {
            var admins = await _context.Usuarios
                .Where(u => u.activo && u.correo != null && u.rol == "Admin")
                .ToListAsync();

            if (idUsuarioAccion.HasValue)
            {
                var usuarioActivo = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.idUsuario == idUsuarioAccion.Value
                                           && u.activo && u.correo != null);
                if (usuarioActivo != null && !admins.Any(a => a.idUsuario == usuarioActivo.idUsuario))
                    admins.Add(usuarioActivo);
            }

            return admins;
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
        private async Task<List<List<string>>> ObtenerFilasMantenimientos(string? buscar, string? estadoFiltro)
        {
            var query = _context.MantenimientosCarros
                .Include(m => m.Carro)
                .Include(m => m.TipoMantenimiento)
                .Include(m => m.UsuarioCreador)
                .AsQueryable();
        
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(m =>
                    m.Carro.Placa.Contains(buscar) ||
                    m.Carro.Marca.Contains(buscar) ||
                    m.TipoMantenimiento.Nombre.Contains(buscar));
        
            if (!string.IsNullOrWhiteSpace(estadoFiltro))
                query = query.Where(m => m.Estado == estadoFiltro);
        
            var mantenimientos = await query.OrderByDescending(m => m.IdMante).ToListAsync();
        
            return mantenimientos.Select(m => new List<string> {
                m.Carro?.Placa ?? "—",
                m.TipoMantenimiento?.Nombre ?? "—",
                m.FechaProgramada.ToString("dd/MM/yyyy"),
                m.Estado ?? "—",
                m.FechaCulminada?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                m.UsuarioCreador?.nombreCompleto ?? m.UsuarioCreador?.username ?? "—"
            }).ToList();
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(string? buscar, string? estadoFiltro)
        {
            var columnas = new List<string> { "Placa", "Tipo Mantenimiento", "Fecha Programada", "Estado", "Fecha Culminación", "Registrado por" };
            var filas = await ObtenerFilasMantenimientos(buscar, estadoFiltro);
            return GenerarCsv(columnas, filas, "Mantenimientos");
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportarPdf(string? buscar, string? estadoFiltro)
        {
            var columnas = new List<string> { "Placa", "Tipo Mant.", "F. Programada", "Estado", "F. Culminación", "Registrado por" };
            var filas = await ObtenerFilasMantenimientos(buscar, estadoFiltro);
            return GenerarPdf("Mantenimientos de Vehículos", columnas, filas);
        }
    }
}
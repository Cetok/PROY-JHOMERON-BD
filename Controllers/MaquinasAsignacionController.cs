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
    public class MaquinaAsignacionesController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public MaquinaAsignacionesController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // INDEX
        public async Task<IActionResult> Index(
            string? nombreMaquina, string? numeroDesde, string? numeroHasta,
            string? estadoOp, string? marca, string? encargado,
            string? areaEspecifica, DateTime? fechaDesde, DateTime? fechaHasta,
            int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.MaquinaAsignaciones
                .Include(a => a.Maquina)
                .Include(a => a.Grupo)
                .Include(a => a.Encargados).ThenInclude(e => e.Empleado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(nombreMaquina))
                query = query.Where(a => a.Maquina.NombreMaquina.Contains(nombreMaquina));

            if (!string.IsNullOrWhiteSpace(numeroDesde) && !string.IsNullOrWhiteSpace(numeroHasta))
                query = query.Where(a =>
                    a.Maquina.NumeroMaquina.CompareTo(numeroDesde) >= 0 &&
                    a.Maquina.NumeroMaquina.CompareTo(numeroHasta) <= 0);
            else if (!string.IsNullOrWhiteSpace(numeroDesde))
                query = query.Where(a => a.Maquina.NumeroMaquina.Contains(numeroDesde));

            if (!string.IsNullOrWhiteSpace(estadoOp))
                query = query.Where(a => a.EstadoOperativo == estadoOp);

            if (!string.IsNullOrWhiteSpace(marca))
                query = query.Where(a => a.Maquina.Marca != null && a.Maquina.Marca.Contains(marca));

            if (!string.IsNullOrWhiteSpace(encargado))
                query = query.Where(a => a.Encargados.Any(e =>
                    (e.Empleado.nombre != null && e.Empleado.nombre.Contains(encargado)) ||
                    (e.Empleado.paterno != null && e.Empleado.paterno.Contains(encargado))));

            if (!string.IsNullOrWhiteSpace(areaEspecifica))
                query = query.Where(a => a.AreaEspecifica != null && a.AreaEspecifica.Contains(areaEspecifica));

            if (fechaDesde.HasValue)
                query = query.Where(a => a.FechaAsignacion.HasValue && a.FechaAsignacion >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(a => a.FechaAsignacion.HasValue && a.FechaAsignacion <= fechaHasta.Value.AddDays(1));

            int total        = await query.CountAsync();
            var asignaciones = await query.OrderBy(a => a.Maquina.NumeroMaquina)
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

            ViewBag.NombreMaquina = nombreMaquina;
            ViewBag.NumeroDesde   = numeroDesde;
            ViewBag.NumeroHasta   = numeroHasta;
            ViewBag.EstadoOp      = estadoOp;
            ViewBag.Marca         = marca;
            ViewBag.Encargado     = encargado;
            ViewBag.AreaEspecifica= areaEspecifica;
            ViewBag.FechaDesde    = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta    = fechaHasta?.ToString("yyyy-MM-dd");
            ViewBag.Pagina        = pagina;
            ViewBag.Total         = total;
            ViewBag.TotalPaginas  = (int)Math.Ceiling((double)total / porPagina);
            return View(asignaciones);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var asig = await _context.MaquinaAsignaciones
                .Include(a => a.Maquina)
                .Include(a => a.Grupo)
                .Include(a => a.Encargados).ThenInclude(e => e.Empleado)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asig == null) return NotFound();
            return View(asig);
        }

        // CREATE GET
        public async Task<IActionResult> Create(int? idMaquina)
        {
            // Bloquear si la máquina está dada de baja
            if (idMaquina.HasValue)
            {
                var maq = await _context.Maquinas.FindAsync(idMaquina.Value);
                if (maq?.Estado == "Dado de Baja")
                {
                    TempData["Error"] = "No se puede crear una asignación para una máquina dada de baja.";
                    return RedirectToAction("Details", "Maquinas", new { id = idMaquina });
                }
            }
            await CargarSelectLists(idMaquina);
            var vm = new MaquinaAsignacion { IdMaquina = idMaquina ?? 0 };
            return View(vm);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaquinaAsignacion asig, List<string> idsEncargados)
        {
            ModelState.Remove("Maquina");
            ModelState.Remove("Grupo");
            ModelState.Remove("Encargado");
            ModelState.Remove("EstadoOperativo");
            ModelState.Remove("IdEmpleadoEncargado");

            var idsValidos = (idsEncargados ?? new List<string>())
                .Where(s => int.TryParse(s, out int v) && v > 0)
                .Select(s => int.Parse(s))
                .Distinct().ToList();

            if (idsValidos.Count > 5)
            {
                ModelState.AddModelError("", "No puede agregar mas de 5 encargados.");
                await CargarSelectLists(asig.IdMaquina);
                return View(asig);
            }

            asig.EsActiva = true;

            if (ModelState.IsValid)
            {
                var asigExistente = await _context.MaquinaAsignaciones
                    .Include(a => a.Encargados).ThenInclude(e => e.Empleado)
                    .FirstOrDefaultAsync(a => a.IdMaquina == asig.IdMaquina && a.EsActiva);

                if (asigExistente != null)
                {
                    var grupoAnterior  = await _context.Grupos.FindAsync(asigExistente.IdGrupo);
                    var encsAnteriores = string.Join(", ", asigExistente.Encargados
                        .Select(e => $"{e.Empleado?.nombre} {e.Empleado?.paterno}"));

                    asigExistente.EsActiva = false;
                    await RegistrarLog(asig.IdMaquina, "CambioAsignacion",
                        $"Grupo: {grupoAnterior?.area} | Encargados: {encsAnteriores}",
                        "Reasignado",
                        "Asignacion anterior cerrada.");
                }

                _context.MaquinaAsignaciones.Add(asig);
                await _context.SaveChangesAsync();

                foreach (var idEmp in idsValidos)
                    _context.MaquinaAsignacionEncargados.Add(new MaquinaAsignacionEncargado
                    {
                        IdAsignacion  = asig.IdAsignacion,
                        IdEmpleado    = idEmp,
                        FechaAgregado = DateTime.Now
                    });
                await _context.SaveChangesAsync();

                var grupo   = await _context.Grupos.FindAsync(asig.IdGrupo);
                var maquina = await _context.Maquinas.FindAsync(asig.IdMaquina);
                var emps    = await _context.Empleados.Where(e => idsValidos.Contains(e.idEmpleado)).ToListAsync();
                var nombres = idsValidos.Any() ? string.Join(", ", emps.Select(e => $"{e.nombre} {e.paterno}")) : "Sin encargado";

                await RegistrarLog(asig.IdMaquina, "CambioAsignacion", "Sin asignacion",
                    $"Grupo: {grupo?.area} | Encargados: {nombres}",
                    asig.Observaciones ?? "Nueva asignacion registrada.");

                await _auditoriaService.RegistrarAsync("Crear", "MaquinaAsignacion", asig.IdAsignacion,
                    $"Asigno maquina {maquina?.NumeroMaquina} al grupo {grupo?.area}");
                await _notifService.NotificarAccionAsync("Creacion", "Asignacion Maquina",
                    $"Maquina {maquina?.NumeroMaquina} asignada al grupo {grupo?.area}",
                    $"/Maquinas/Details/{asig.IdMaquina}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _mq) ? _mq : null);

                TempData["Success"] = "Maquina asignada correctamente.";
                return RedirectToAction("Details", "Maquinas", new { id = asig.IdMaquina });
            }

            await CargarSelectLists(asig.IdMaquina);
            return View(asig);
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var asig = await _context.MaquinaAsignaciones
                .Include(a => a.Maquina)
                .Include(a => a.Encargados).ThenInclude(e => e.Empleado)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);
            if (asig == null) return NotFound();

            ViewBag.EncargadosActuales = asig.Encargados.Select(e => e.IdEmpleado).ToList();
            await CargarSelectLists(asig.IdMaquina);
            return View(asig);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaquinaAsignacion asig, List<string> idsEncargados)
        {
            if (id != asig.IdAsignacion) return NotFound();
            ModelState.Remove("Maquina");
            ModelState.Remove("Grupo");
            ModelState.Remove("Encargado");
            ModelState.Remove("IdEmpleadoEncargado");

            var idsValidos = (idsEncargados ?? new List<string>())
                .Where(s => int.TryParse(s, out int v) && v > 0)
                .Select(s => int.Parse(s))
                .Distinct().ToList();
            if (idsValidos.Count > 5)
            {
                ModelState.AddModelError("", "No puede agregar mas de 5 encargados.");
                ViewBag.EncargadosActuales = idsValidos;
                await CargarSelectLists(asig.IdMaquina);
                return View(asig);
            }

            if (ModelState.IsValid)
            {
                var original = await _context.MaquinaAsignaciones.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.IdAsignacion == id);
                try
                {
                    _context.Update(asig);

                    var actuales = await _context.MaquinaAsignacionEncargados
                        .Where(e => e.IdAsignacion == id).ToListAsync();
                    _context.MaquinaAsignacionEncargados.RemoveRange(actuales);

                    foreach (var idEmp in idsValidos)
                        _context.MaquinaAsignacionEncargados.Add(new MaquinaAsignacionEncargado
                        {
                            IdAsignacion  = id,
                            IdEmpleado    = idEmp,
                            FechaAgregado = DateTime.Now
                        });

                    await _context.SaveChangesAsync();

                    var idsAnteriores = actuales.Select(e => e.IdEmpleado).OrderBy(x => x).ToList();
                    if (!idsAnteriores.SequenceEqual(idsValidos.OrderBy(x => x).ToList()))
                    {
                        var empsNuevos    = await _context.Empleados.Where(e => idsValidos.Contains(e.idEmpleado)).ToListAsync();
                        var nombresNuevos = string.Join(", ", empsNuevos.Select(e => $"{e.nombre} {e.paterno}"));
                        await RegistrarLog(asig.IdMaquina, "CambioEncargado",
                            "Encargados anteriores", nombresNuevos, "Cambio de encargados.");
                    }

                    if (original?.IdGrupo != asig.IdGrupo)
                    {
                        var grupoAnterior = await _context.Grupos.FindAsync(original?.IdGrupo);
                        var grupoNuevo    = await _context.Grupos.FindAsync(asig.IdGrupo);
                        await RegistrarLog(asig.IdMaquina, "CambioAsignacion",
                            grupoAnterior?.area ?? "—", grupoNuevo?.area ?? "—", "Reasignacion a otro grupo.");
                    }

                    await _auditoriaService.RegistrarAsync("Editar", "MaquinaAsignacion", id, $"Edito asignacion #{id}");
                    TempData["Success"] = "Asignacion actualizada correctamente.";
                    return RedirectToAction("Details", "Maquinas", new { id = asig.IdMaquina });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.MaquinaAsignaciones.AnyAsync(a => a.IdAsignacion == id)) return NotFound();
                    throw;
                }
            }

            ViewBag.EncargadosActuales = idsValidos;
            await CargarSelectLists(asig.IdMaquina);
            return View(asig);
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
        private async Task<List<List<string>>> ObtenerFilasMaquinaAsignaciones(
            string? nombreMaquina, string? numeroDesde, string? numeroHasta,
            string? estadoOp, string? marca, string? encargado,
            string? areaEspecifica, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var query = _context.MaquinaAsignaciones
                .Include(a => a.Maquina)
                .Include(a => a.Grupo)
                .Include(a => a.Encargados).ThenInclude(e => e.Empleado)
                .AsQueryable();
        
            if (!string.IsNullOrWhiteSpace(nombreMaquina))
                query = query.Where(a => a.Maquina.NombreMaquina.Contains(nombreMaquina));
            if (!string.IsNullOrWhiteSpace(numeroDesde) && !string.IsNullOrWhiteSpace(numeroHasta))
                query = query.Where(a => a.Maquina.NumeroMaquina.CompareTo(numeroDesde) >= 0 && a.Maquina.NumeroMaquina.CompareTo(numeroHasta) <= 0);
            else if (!string.IsNullOrWhiteSpace(numeroDesde))
                query = query.Where(a => a.Maquina.NumeroMaquina.Contains(numeroDesde));
            if (!string.IsNullOrWhiteSpace(estadoOp))
                query = query.Where(a => a.EstadoOperativo == estadoOp);
            if (!string.IsNullOrWhiteSpace(marca))
                query = query.Where(a => a.Maquina.Marca != null && a.Maquina.Marca.Contains(marca));
            if (!string.IsNullOrWhiteSpace(encargado))
                query = query.Where(a => a.Encargados.Any(e =>
                    (e.Empleado.nombre != null && e.Empleado.nombre.Contains(encargado)) ||
                    (e.Empleado.paterno != null && e.Empleado.paterno.Contains(encargado))));
            if (!string.IsNullOrWhiteSpace(areaEspecifica))
                query = query.Where(a => a.AreaEspecifica != null && a.AreaEspecifica.Contains(areaEspecifica));
            if (fechaDesde.HasValue)
                query = query.Where(a => a.FechaAsignacion.HasValue && a.FechaAsignacion >= fechaDesde.Value);
            if (fechaHasta.HasValue)
                query = query.Where(a => a.FechaAsignacion.HasValue && a.FechaAsignacion <= fechaHasta.Value.AddDays(1));
        
            var asignaciones = await query.OrderBy(a => a.Maquina.NumeroMaquina).ToListAsync();
        
            return asignaciones.Select(a => new List<string> {
                a.Maquina?.NumeroCompleto ?? "—",
                a.Maquina?.NombreMaquina ?? "—",
                a.Grupo?.area ?? "Sin asignar",
                a.Encargados?.Any() == true ? string.Join(", ", a.Encargados.Select(e => e.Empleado?.nombre + " " + e.Empleado?.paterno)) : "Sin encargado",
                a.EstadoOperativo ?? "—",
                a.FechaAsignacion?.ToString("dd/MM/yyyy") ?? "—"
            }).ToList();
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(
            string? nombreMaquina, string? numeroDesde, string? numeroHasta,
            string? estadoOp, string? marca, string? encargado,
            string? areaEspecifica, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var columnas = new List<string> { "N° Máquina", "Nombre", "Grupo", "Encargado(s)", "Estado Operativo", "Fecha Asignación" };
            var filas = await ObtenerFilasMaquinaAsignaciones(nombreMaquina, numeroDesde, numeroHasta, estadoOp, marca, encargado, areaEspecifica, fechaDesde, fechaHasta);
            return GenerarCsv(columnas, filas, "Asignaciones_Maquinas");
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportarPdf(
            string? nombreMaquina, string? numeroDesde, string? numeroHasta,
            string? estadoOp, string? marca, string? encargado,
            string? areaEspecifica, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var columnas = new List<string> { "N° Máquina", "Nombre", "Grupo", "Encargado(s)", "Estado Op.", "Fecha Asig." };
            var filas = await ObtenerFilasMaquinaAsignaciones(nombreMaquina, numeroDesde, numeroHasta, estadoOp, marca, encargado, areaEspecifica, fechaDesde, fechaHasta);
            return GenerarPdf("Asignaciones de Máquinas", columnas, filas);
        }

        // HELPERS
        private async Task CargarSelectLists(int? idMaquinaSeleccionada = null)
        {
            var maquinas  = await _context.Maquinas.OrderBy(m => m.NumeroMaquina).ToListAsync();
            var grupos    = await _context.Grupos.OrderBy(g => g.area).ToListAsync();
            var empleados = await _context.Empleados
                .Where(e => e.estado == "Activo")
                .OrderBy(e => e.paterno)
                .Select(e => new { e.idEmpleado, Nombre = e.nombre + " " + e.paterno + " " + e.materno })
                .ToListAsync();

            ViewBag.MaquinasList  = new SelectList(maquinas, "IdMaquina", "NombreMaquina", idMaquinaSeleccionada);
            ViewBag.GruposList    = new SelectList(grupos, "idGrupo", "area");
            ViewBag.EmpleadosList = empleados;
        }

        private async Task RegistrarLog(int idMaquina, string tipoEvento,
            string? valorAnterior, string? valorNuevo, string? observaciones)
        {
            var idStr  = HttpContext.Session.GetString("UsuarioId");
            var nombre = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            _context.MaquinaLogs.Add(new MaquinaLog
            {
                IdMaquina     = idMaquina,
                IdUsuario     = idUsuario,
                NombreUsuario = nombre,
                TipoEvento    = tipoEvento,
                ValorAnterior = valorAnterior,
                ValorNuevo    = valorNuevo,
                Observaciones = observaciones,
                FechaHora     = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }
}
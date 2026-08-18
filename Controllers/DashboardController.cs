using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;

namespace PROYJHOME2026.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        public DashboardController(AppDbContext context) { _context = context; }

        private string Rol      => HttpContext.Session.GetString("UsuarioRol")      ?? "";
        private string Username => HttpContext.Session.GetString("UsuarioUsername") ?? "";
        private bool   EsAdmin  => Rol == "Admin" || Username.Equals("danitza", StringComparison.OrdinalIgnoreCase);

        // ── INDEX ─────────────────────────────────────────────────
        public IActionResult Index()
        {
            var rol = Rol;
            if (string.IsNullOrEmpty(rol)) return RedirectToAction("Login", "Auth");

            // Tab inicial según rol
            ViewBag.TabInicial = rol switch
            {
                "SoporteTI"  => "ti",
                "Transporte" => "flota",
                "Produccion" => "produccion",
                "SSOMA"      => "ssoma",
                "Logistica"  => "logistica",
                _            => "resumen"   // Admin y danitza
            };

            ViewBag.EsAdmin    = EsAdmin;
            ViewBag.Rol        = rol;
            return View();
        }

        // ── RESUMEN GENERAL (solo Admin/Danitza) ──────────────────
        [HttpGet]
        public async Task<IActionResult> DatosResumen()
        {
            if (!EsAdmin) return Forbid();

            var totalEmpleados   = await _context.Empleados.CountAsync();
            var empleadosActivos = await _context.Empleados.CountAsync(e => e.estado == "Activo");
            var totalEquipos     = await _context.Equipos.CountAsync();
            var equiposAsignados = await _context.Asignaciones
                .Where(a => a.EstadoAsignacion == "Activo")
                .Select(a => a.IdEquipo).Distinct().CountAsync();
            var totalCarros      = await _context.Carros.CountAsync();
            var carrosActivos    = await _context.Carros.CountAsync(c => c.Estado == "Activo");
            var totalMaquinas    = await _context.Maquinas.CountAsync();
            var maquinasActivas  = await _context.Maquinas.CountAsync(m => m.Estado == "Activo");
            var totalChips       = await _context.Chips.Include(c => c.Asignaciones).CountAsync();
            var chipsAsignados   = await _context.Chips
                .Include(c => c.Asignaciones)
                .CountAsync(c => c.Asignaciones.Any(a => a.EstadoAsignacion == "Activo"));
            var mantePendientes  = await _context.MantenimientosCarros.CountAsync(m => m.Estado == "Pendiente");
            var manteEnProceso   = await _context.MantenimientosCarros.CountAsync(m => m.Estado == "En proceso");

            // Actividad últimos 6 meses
            var hace6Meses = DateTime.Today.AddMonths(-5);
            var actividadMensual = new List<object>();
            for (int i = 0; i < 6; i++)
            {
                var mes = hace6Meses.AddMonths(i);
                var ini = new DateTime(mes.Year, mes.Month, 1);
                var fin = ini.AddMonths(1);
                var asigs  = await _context.Asignaciones.CountAsync(a => a.FechaAsignacion >= ini && a.FechaAsignacion < fin);
                var mantes = await _context.MantenimientosCarros.CountAsync(m => m.FechaProgramada >= ini && m.FechaProgramada < fin);
                actividadMensual.Add(new {
                    mes   = mes.ToString("MMM", new System.Globalization.CultureInfo("es-PE")),
                    asigs, mantes
                });
            }

            // Últimos movimientos del sistema (máx 100, paginados de a 10)
            const int porPaginaMov = 10;
            const int maxMov       = 100;
            var totalMovReal = await _context.AuditoriaLogs.CountAsync();
            var totalMov     = Math.Min(totalMovReal, maxMov);

            var ultimosMovimientos = await _context.AuditoriaLogs
                .OrderByDescending(l => l.FechaHora)
                .Take(maxMov)
                .Take(porPaginaMov)
                .Select(l => new {
                    fecha       = l.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    l.Accion,
                    l.Entidad,
                    l.Descripcion,
                    l.NombreUsuario
                })
                .ToListAsync();

            return Json(new {
                kpis = new {
                    totalEmpleados, empleadosActivos,
                    totalEquipos, equiposAsignados,
                    totalCarros, carrosActivos,
                    totalMaquinas, maquinasActivas,
                    totalChips, chipsAsignados,
                    mantePendientes, manteEnProceso
                },
                actividadMensual,
                ultimosMovimientos,
                totalMovimientos      = totalMov,
                totalPaginasMov       = (int)Math.Ceiling((double)totalMov / porPaginaMov)
            });
        }

        // ── AJAX: movimientos del sistema paginados (para el dashboard) ──
        [HttpGet]
        public async Task<IActionResult> MovimientosData(int pagina = 1)
        {
            if (!EsAdmin) return Forbid();

            const int porPagina  = 10;
            const int maxPaginas = 10;
            const int totalMax   = porPagina * maxPaginas; // máx 100

            var totalReal = await _context.AuditoriaLogs.CountAsync();
            var total     = Math.Min(totalReal, totalMax);

            var movimientos = await _context.AuditoriaLogs
                .OrderByDescending(l => l.FechaHora)
                .Take(totalMax)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .Select(l => new {
                    fecha        = l.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    l.Accion,
                    l.Entidad,
                    l.Descripcion,
                    l.NombreUsuario
                })
                .ToListAsync();

            return Json(new {
                total,
                pagina,
                totalPaginas = (int)Math.Ceiling((double)total / porPagina),
                registros    = movimientos
            });
        }

        // ── FLOTA VEHICULAR ───────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DatosFlota()
        {
            if (Rol != "Admin" && Rol != "Transporte" && Rol != "SSOMA" && !EsAdmin) return Forbid();

            var carros = await _context.Carros.ToListAsync();
            var estadoCarros = carros.GroupBy(c => c.Estado)
                .Select(g => new { estado = g.Key, total = g.Count() }).ToList();

            var categoriaCarros = carros
                .GroupBy(c => c.Categoria ?? "Sin categoría")
                .Select(g => new { cat = g.Key, total = g.Count() }).ToList();

            // Mantenimientos por mes (últimos 6)
            var hace6 = DateTime.Today.AddMonths(-5);
            var mantesPorMes = new List<object>();
            for (int i = 0; i < 6; i++)
            {
                var mes = hace6.AddMonths(i);
                var ini = new DateTime(mes.Year, mes.Month, 1);
                var fin = ini.AddMonths(1);
                var total = await _context.MantenimientosCarros
                    .CountAsync(m => m.FechaProgramada >= ini && m.FechaProgramada < fin);
                mantesPorMes.Add(new { mes = mes.ToString("MMM yyyy", new System.Globalization.CultureInfo("es-PE")), total });
            }

            // Top 5 carros con más mantenimientos
            var topCarros = await _context.MantenimientosCarros
                .Include(m => m.Carro)
                .GroupBy(m => new { m.IdCarro, m.Carro!.Placa })
                .Select(g => new { placa = g.Key.Placa, total = g.Count() })
                .OrderByDescending(x => x.total).Take(5).ToListAsync();

            // Mantenimientos por estado
            var manteEstado = await _context.MantenimientosCarros
                .GroupBy(m => m.Estado)
                .Select(g => new { estado = g.Key, total = g.Count() }).ToListAsync();

            // Vencimientos próximos (30 días)
            var hoy    = DateTime.Today;
            var en30   = hoy.AddDays(30);
            var vencimientosSeguros = await _context.CarroSeguros
                .Include(cs => cs.Carro).Include(cs => cs.Seguro)
                .Where(cs => cs.FechaCulminada.HasValue
                          && cs.FechaCulminada.Value.Date >= hoy
                          && cs.FechaCulminada.Value.Date <= en30)
                .OrderBy(cs => cs.FechaCulminada)
                .Select(cs => new {
                    placa    = cs.Carro!.Placa,
                    tipo     = "Seguro " + cs.Seguro!.TipoSeguro,
                    fecha    = cs.FechaCulminada!.Value.ToString("dd/MM/yyyy"),
                    diasRest = (cs.FechaCulminada!.Value.Date - hoy).Days
                }).ToListAsync();

            var vencimientosModal = await _context.CarroModalidades
                .Include(cm => cm.Carro).Include(cm => cm.Modalidad)
                .Where(cm => cm.FechaVencimiento.HasValue
                          && cm.FechaVencimiento.Value.Date >= hoy
                          && cm.FechaVencimiento.Value.Date <= en30)
                .OrderBy(cm => cm.FechaVencimiento)
                .Select(cm => new {
                    placa    = cm.Carro!.Placa,
                    tipo     = "Modalidad " + cm.Modalidad!.TipoModalidad,
                    fecha    = cm.FechaVencimiento!.Value.ToString("dd/MM/yyyy"),
                    diasRest = (cm.FechaVencimiento!.Value.Date - hoy).Days
                }).ToListAsync();

            var vencimientosHab = await _context.HabilitacionesVehiculares
                .Include(h => h.Carro)
                .Where(h => h.EsVigente
                         && h.FechaCulminacion.Date >= hoy
                         && h.FechaCulminacion.Date <= en30)
                .OrderBy(h => h.FechaCulminacion)
                .Select(h => new {
                    placa    = h.Carro!.Placa,
                    tipo     = "Habilitación [" + h.Codigo + "]",
                    fecha    = h.FechaCulminacion.ToString("dd/MM/yyyy"),
                    diasRest = (h.FechaCulminacion.Date - hoy).Days
                }).ToListAsync();

            var vencimientos = vencimientosSeguros
                .Concat(vencimientosModal)
                .Concat(vencimientosHab)
                .OrderBy(v => v.diasRest).ToList();

            // Mantenimientos pendientes próximos
            var mantePendientes = await _context.MantenimientosCarros
                .Include(m => m.Carro).Include(m => m.TipoMantenimiento)
                .Where(m => m.Estado == "Pendiente" && m.FechaProgramada >= hoy)
                .OrderBy(m => m.FechaProgramada)
                .Take(10)
                .Select(m => new {
                    placa     = m.Carro!.Placa,
                    tipo      = m.TipoMantenimiento!.Nombre,
                    fecha     = m.FechaProgramada.ToString("dd/MM/yyyy"),
                    diasRest  = (m.FechaProgramada.Date - hoy).Days
                }).ToListAsync();

            return Json(new {
                estadoCarros, categoriaCarros,
                mantesPorMes, topCarros,
                manteEstado, vencimientos,
                mantePendientes,
                totalCarros    = carros.Count,
                carrosActivos  = carros.Count(c => c.Estado == "Activo"),
                manteTotal     = await _context.MantenimientosCarros.CountAsync(),
                mantePendTotal = await _context.MantenimientosCarros.CountAsync(m => m.Estado == "Pendiente")
            });
        }

        // ── EQUIPOS TI ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DatosTI()
        {
            if (Rol != "Admin" && Rol != "SoporteTI" && Rol != "Logistica" && !EsAdmin) return Forbid();

            var equipos = await _context.Equipos.Include(e => e.TipoEquipo).ToListAsync();

            var porTipo = equipos
                .GroupBy(e => e.TipoEquipo?.tipo ?? "Sin tipo")
                .Select(g => new { tipo = g.Key, total = g.Count() })
                .OrderByDescending(x => x.total).ToList();

            var porEstado = equipos
                .GroupBy(e => e.estado_equipo)
                .Select(g => new { estado = g.Key, total = g.Count() }).ToList();

            // Asignaciones activas vs inactivas
            var asigActivas   = await _context.Asignaciones.CountAsync(a => a.EstadoAsignacion == "Activo");
            var asigInactivas = await _context.Asignaciones.CountAsync(a => a.EstadoAsignacion == "Inactivo");

            // Equipos sin asignar
            var equiposSinAsignar = equipos.Count(e => e.estado_equipo == "Activo");

            // Últimas 10 asignaciones
            var ultimasAsig = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo).ThenInclude(e => e.TipoEquipo)
                .Include(a => a.Grupo)
                .OrderByDescending(a => a.IdAsignacion)
                .Take(10)
                .Select(a => new {
                    empleado = a.Empleado!.nombre + " " + a.Empleado.paterno,
                    equipo   = a.Equipo!.TipoEquipo!.tipo + " — " + (a.Equipo.NombrePc ?? a.Equipo.marca + " " + a.Equipo.modelo),
                    area     = a.Grupo != null ? a.Grupo.area : "—",
                    estado   = a.EstadoAsignacion,
                    fecha    = a.FechaAsignacion.ToString("dd/MM/yyyy")
                }).ToListAsync();

            // Asignaciones por mes (últimos 6)
            var hace6 = DateTime.Today.AddMonths(-5);
            var asigsPorMes = new List<object>();
            for (int i = 0; i < 6; i++)
            {
                var mes = hace6.AddMonths(i);
                var ini = new DateTime(mes.Year, mes.Month, 1);
                var fin = ini.AddMonths(1);
                var total = await _context.Asignaciones.CountAsync(a => a.FechaAsignacion >= ini && a.FechaAsignacion < fin);
                asigsPorMes.Add(new { mes = mes.ToString("MMM", new System.Globalization.CultureInfo("es-PE")), total });
            }

            return Json(new {
                porTipo, porEstado,
                asigActivas, asigInactivas,
                equiposSinAsignar,
                totalEquipos = equipos.Count,
                ultimasAsig, asigsPorMes
            });
        }

        // ── PRODUCCIÓN ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DatosProduccion()
        {
            if (Rol != "Admin" && Rol != "Produccion" && !EsAdmin) return Forbid();

            var maquinas = await _context.Maquinas
                .Include(m => m.Asignaciones).ThenInclude(a => a.Encargados).ThenInclude(e => e.Empleado)
                .ToListAsync();

            var porEstado = maquinas.GroupBy(m => m.Estado)
                .Select(g => new { estado = g.Key, total = g.Count() }).ToList();

            var porNombre = maquinas.GroupBy(m => m.NombreMaquina)
                .Select(g => new { nombre = g.Key, total = g.Count() })
                .OrderByDescending(x => x.total).Take(10).ToList();

            var sinAsignar = maquinas.Count(m => m.AsignacionActual == null && m.Estado != "Dado de Baja");

            // Últimos cambios de estado (logs)
            var ultimosCambios = await _context.AuditoriaLogs
                .Where(l => l.Entidad == "Maquina")
                .OrderByDescending(l => l.FechaHora)
                .Take(10)
                .Select(l => new {
                    accion      = l.Accion,
                    descripcion = l.Descripcion,
                    usuario     = l.NombreUsuario ?? "—",
                    fecha       = l.FechaHora.ToString("dd/MM/yyyy HH:mm")
                }).ToListAsync();

            // Encargados con más máquinas
            var encargados = maquinas
                .Where(m => m.AsignacionActual?.Encargados != null)
                .SelectMany(m => m.AsignacionActual!.Encargados
                    .Select(e => e.Empleado?.nombre + " " + e.Empleado?.paterno))
                .Where(n => n != null)
                .GroupBy(n => n)
                .Select(g => new { nombre = g.Key, total = g.Count() })
                .OrderByDescending(x => x.total).Take(8).ToList();

            // Detalle máquinas — últimas 10 registradas
            var detalleMaquinas = maquinas
                .Where(m => m.Estado != "Dado de Baja")
                .OrderByDescending(m => m.FechaRegistro)
                .Take(10)
                .Select(m => new {
                    numero    = m.NumeroCompleto,
                    nombre    = m.NombreMaquina,
                    marca     = m.Marca ?? "—",
                    estado    = m.Estado,
                    encargado = m.AsignacionActual?.Encargados.Any() == true
                        ? string.Join(", ", m.AsignacionActual.Encargados.Select(e => e.Empleado?.nombre + " " + e.Empleado?.paterno))
                        : "Sin asignar"
                }).ToList();

            return Json(new {
                porEstado, porNombre,
                sinAsignar, ultimosCambios,
                encargados, detalleMaquinas,
                totalMaquinas   = maquinas.Count,
                maquinasActivas = maquinas.Count(m => m.Estado == "Activo")
            });
        }

        // ── LOGÍSTICA / CHIPS ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DatosLogistica()
        {
            if (Rol != "Admin" && Rol != "Logistica" && !EsAdmin) return Forbid();

            var chips = await _context.Chips
                .Include(c => c.Asignaciones).ThenInclude(a => a.Empleado)
                .ToListAsync();

            var asignados  = chips.Count(c => c.Asignaciones.Any(a => a.EstadoAsignacion == "Activo"));
            var libres     = chips.Count - asignados;

            // Historial asignaciones de chips por mes (últimos 6)
            var hace6 = DateTime.Today.AddMonths(-5);
            var historialMensual = new List<object>();
            for (int i = 0; i < 6; i++)
            {
                var mes = hace6.AddMonths(i);
                var ini = new DateTime(mes.Year, mes.Month, 1);
                var fin = ini.AddMonths(1);
                var total = await _context.Asignaciones
                    .CountAsync(a => a.IdChip != null && a.FechaAsignacion >= ini && a.FechaAsignacion < fin);
                historialMensual.Add(new { mes = mes.ToString("MMM", new System.Globalization.CultureInfo("es-PE")), total });
            }

            // Estado actual de todos los chips
            var tablaChips = chips.OrderBy(c => c.NumeroCelular).Select(c => {
                var asigActiva = c.Asignaciones.FirstOrDefault(a => a.EstadoAsignacion == "Activo");
                return new {
                    numero   = c.NumeroCelular,
                    estado   = asigActiva != null ? "Asignado" : "Libre",
                    empleado = asigActiva != null
                        ? (asigActiva.Empleado?.nombre + " " + asigActiva.Empleado?.paterno)
                        : "—",
                    fecha    = asigActiva?.FechaAsignacion.ToString("dd/MM/yyyy") ?? "—"
                };
            }).ToList();

            return Json(new {
                asignados, libres,
                totalChips = chips.Count,
                historialMensual, tablaChips
            });
        }

        // ── SSOMA ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DatosSSoma()
        {
            if (Rol != "Admin" && Rol != "SSOMA" && Rol != "Transporte" && !EsAdmin) return Forbid();

            var hoy  = DateTime.Today;
            var en30 = hoy.AddDays(30);

            var asesorios = await _context.CarroAsesorios
                .Include(ca => ca.Asesorio).Include(ca => ca.Carro).ToListAsync();

            var extintores = asesorios.Where(a =>
                a.Asesorio?.TipoAsesorio?.Contains("Extintor") == true).ToList();
            var botiquines = asesorios.Where(a =>
                a.Asesorio?.TipoAsesorio?.Contains("Botiquín") == true ||
                a.Asesorio?.TipoAsesorio?.Contains("Botiquin") == true).ToList();

            // Extintores por vencer (30 días)
            var extintosPorVencer = extintores
                .Where(e => e.FechaVencimientoExtintor.HasValue
                         && e.FechaVencimientoExtintor.Value.ToDateTime(TimeOnly.MinValue) >= hoy
                         && e.FechaVencimientoExtintor.Value.ToDateTime(TimeOnly.MinValue) <= en30)
                .OrderBy(e => e.FechaVencimientoExtintor)
                .Select(e => new {
                    placa    = e.Carro?.Placa ?? "—",
                    tipo     = e.TipoExtintor ?? "—",
                    peso     = e.PesoExtintor ?? "—",
                    fecha    = e.FechaVencimientoExtintor!.Value.ToString("dd/MM/yyyy"),
                    diasRest = (e.FechaVencimientoExtintor.Value.ToDateTime(TimeOnly.MinValue) - hoy).Days
                }).ToList();

            // Todos los extintores con estado
            var tablaExtintores = extintores.Select(e => {
                var diasRest = e.FechaVencimientoExtintor.HasValue
                    ? (e.FechaVencimientoExtintor.Value.ToDateTime(TimeOnly.MinValue) - hoy).Days
                    : (int?)null;
                return new {
                    placa    = e.Carro?.Placa ?? "—",
                    tipo     = e.TipoExtintor ?? "—",
                    peso     = e.PesoExtintor ?? "—",
                    fecha    = e.FechaVencimientoExtintor?.ToString("dd/MM/yyyy") ?? "Sin fecha",
                    diasRest,
                    estado   = diasRest == null ? "Sin fecha"
                             : diasRest < 0    ? "Vencido"
                             : diasRest <= 7   ? "Crítico"
                             : diasRest <= 30  ? "Próximo"
                             : "Vigente"
                };
            }).OrderBy(e => e.diasRest ?? 999).ToList();

            // Botiquines por carro
            var botiquinesPorCarro = botiquines
                .GroupBy(b => b.Carro?.Placa ?? "—")
                .Select(g => new { placa = g.Key, total = g.Count() })
                .OrderByDescending(x => x.total).ToList();

            // Conteo extintores por estado
            var extinEstado = new {
                vencidos = tablaExtintores.Count(e => e.estado == "Vencido"),
                criticos = tablaExtintores.Count(e => e.estado == "Crítico"),
                proximos = tablaExtintores.Count(e => e.estado == "Próximo"),
                vigentes = tablaExtintores.Count(e => e.estado == "Vigente"),
                sinFecha = tablaExtintores.Count(e => e.estado == "Sin fecha")
            };

            // Inspecciones recientes de botiquín (campo correcto: FechaInspeccion, sin Observaciones en header)
            var inspBotiquin = await _context.InspeccionBotiquinTransportes
                .Include(i => i.Carro)
                .OrderByDescending(i => i.FechaInspeccion)
                .Take(5)
                .ToListAsync();
            var listaBotiquin = inspBotiquin.Select(i => new {
                tipo  = "Botiquín",
                placa = i.Carro?.Placa ?? "—",
                fecha = i.FechaInspeccion.ToString("dd/MM/yyyy"),
                obs   = i.InspeccionadoPor ?? "—"
            }).ToList();

            // Inspecciones de extintor (no tiene Carro directo — usa Asesorio)
            var inspExtintor = await _context.InspeccionExtintores
                .Include(i => i.Asesorio)
                .OrderByDescending(i => i.FechaInspeccion)
                .Take(5)
                .ToListAsync();
            var listaExtintor = inspExtintor.Select(i => new {
                tipo  = "Extintor",
                placa = i.Asesorio?.TipoAsesorio ?? "—",
                fecha = i.FechaInspeccion.ToString("dd/MM/yyyy"),
                obs   = i.InspeccionadoPor ?? "—"
            }).ToList();

            var inspeccionesRecientes = listaBotiquin
                .Concat(listaExtintor)
                .OrderByDescending(i => i.fecha)
                .Take(10).ToList();

            // Checklists — campo correcto: FechaInspeccion (no FechaRevision)
            var totalChecklists = await _context.CheckListTransportes.CountAsync();
            var fechaHace30     = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
            var checklistsMes   = await _context.CheckListTransportes
                .CountAsync(c => c.FechaInspeccion >= fechaHace30);

            return Json(new {
                totalExtintores = extintores.Count,
                totalBotiquines = botiquines.Count,
                extinEstado, extintosPorVencer,
                tablaExtintores, botiquinesPorCarro,
                inspeccionesRecientes,
                totalChecklists, checklistsMes
            });
        }

        // ── RR.HH (solo Admin/Danitza) ────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DatosRRHH()
        {
            if (!EsAdmin) return Forbid();

            var empleados = await _context.Empleados.ToListAsync();
            var grupos    = await _context.Grupos.ToListAsync();

            var porEstado = empleados
                .GroupBy(e => e.estado ?? "Sin estado")
                .Select(g => new { estado = g.Key, total = g.Count() }).ToList();

            var porCargo = empleados
                .GroupBy(e => e.Cargo ?? "Sin cargo")
                .Select(g => new { cargo = g.Key, total = g.Count() })
                .OrderByDescending(x => x.total).Take(10).ToList();

            // Empleados por grupo usando Asignaciones
            var asignacionesPorGrupo = await _context.Asignaciones
                .Include(a => a.Grupo)
                .Where(a => a.EstadoAsignacion == "Activo" && a.IdGrupo != null)
                .GroupBy(a => a.Grupo!.area ?? "—")
                .Select(g => new { area = g.Key, total = g.Select(a => a.IdEmpleado).Distinct().Count() })
                .OrderByDescending(x => x.total)
                .ToListAsync();

            var porGrupo = asignacionesPorGrupo.Any()
                ? asignacionesPorGrupo.Select(g => new { area = g.area, total = g.total }).ToList<object>()
                : grupos.Select(g => new { area = g.area ?? "—", total = 0 }).ToList<object>();

            // Últimos 10 registrados
            var ultimosRegistrados = empleados
                .OrderByDescending(e => e.idEmpleado)
                .Take(10)
                .Select(e => new {
                    nombre = e.nombre + " " + e.paterno,
                    cargo  = e.Cargo ?? "—",
                    estado = e.estado ?? "—",
                    dni    = e.dni ?? "—"
                }).ToList();

            return Json(new {
                porEstado, porCargo, porGrupo,
                ultimosRegistrados,
                totalEmpleados   = empleados.Count,
                empleadosActivos = empleados.Count(e => e.estado == "Activo"),
                totalGrupos      = grupos.Count
            });
        }
    }
}
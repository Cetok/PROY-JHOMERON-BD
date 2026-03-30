using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class EquiposController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        private static readonly string[] TiposTecnicos  = { "CPU", "LAPTOP" };
        private static readonly string   TipoPcCompleto  = "PC COMPLETO";

        public EquiposController(
            AppDbContext        context,
            AuditoriaService    auditoriaService,
            NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estado, int? tipoId, int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.Equipos.Include(e => e.TipoEquipo).AsQueryable();
 
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    // Campos estándar
                    (e.marca             != null && e.marca.Contains(buscar))             ||
                    (e.modelo            != null && e.modelo.Contains(buscar))            ||
                    (e.numero_serie      != null && e.numero_serie.Contains(buscar))      ||
                    (e.sistema_operativo != null && e.sistema_operativo.Contains(buscar)) ||
                    // Nombre PC Completo
                    (e.NombrePc          != null && e.NombrePc.Contains(buscar))          ||
                    // Componentes de PC Completo — CPU
                    (e.PcCpuMarca        != null && e.PcCpuMarca.Contains(buscar))        ||
                    (e.PcCpuModelo       != null && e.PcCpuModelo.Contains(buscar))       ||
                    (e.PcCpuSerie        != null && e.PcCpuSerie.Contains(buscar))        ||
                    (e.PcCpuProcesador   != null && e.PcCpuProcesador.Contains(buscar))   ||
                    // Componentes de PC Completo — Monitor
                    (e.PcMonitorMarca    != null && e.PcMonitorMarca.Contains(buscar))    ||
                    (e.PcMonitorModelo   != null && e.PcMonitorModelo.Contains(buscar))   ||
                    (e.PcMonitorSerie    != null && e.PcMonitorSerie.Contains(buscar))    ||
                    // Componentes de PC Completo — Mouse
                    (e.PcMouseMarca      != null && e.PcMouseMarca.Contains(buscar))      ||
                    (e.PcMouseModelo     != null && e.PcMouseModelo.Contains(buscar))     ||
                    (e.PcMouseSerie      != null && e.PcMouseSerie.Contains(buscar))      ||
                    // Componentes de PC Completo — Teclado
                    (e.PcTecladoMarca    != null && e.PcTecladoMarca.Contains(buscar))    ||
                    (e.PcTecladoModelo   != null && e.PcTecladoModelo.Contains(buscar))   ||
                    (e.PcTecladoSerie    != null && e.PcTecladoSerie.Contains(buscar)));
 
            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado == "otros")
                    query = query.Where(e =>
                        e.estado_equipo != "Activo" &&
                        e.estado_equipo != "Asignado" &&
                        e.estado_equipo != "Mantenimiento");
                else
                    query = query.Where(e => e.estado_equipo == estado);
            }
 
            if (tipoId.HasValue)
            {
                // Verificar si el tipo seleccionado es un componente de PC Completo
                var tipoSeleccionado = await _context.TiposEquipo.FindAsync(tipoId.Value);
                var tipoNombreUpper  = tipoSeleccionado?.tipo?.ToUpper().Trim() ?? "";
                var componentesPc    = new[] { "CPU", "MONITOR", "MOUSE", "TECLADO", "MOUSEPAD" };
                bool esComponentePc  = componentesPc.Any(c => tipoNombreUpper.Contains(c))
                                       && !tipoNombreUpper.Contains("PC COMPLETO");
 
                if (esComponentePc)
                {
                    // Mostrar equipos de ese tipo + PC Completo que tienen ese componente
                    var idPcCompleto = await _context.TiposEquipo
                        .Where(t => t.tipo != null && t.tipo.ToUpper().Contains("PC COMPLETO"))
                        .Select(t => t.idTipoEquipo)
                        .FirstOrDefaultAsync();
 
                    query = query.Where(e =>
                        e.idTipoEquipo == tipoId ||
                        e.idTipoEquipo == idPcCompleto);
                }
                else
                {
                    query = query.Where(e => e.idTipoEquipo == tipoId);
                }
            }
 
            int total   = await query.CountAsync();
            var equipos = await query.OrderByDescending(e => e.idEquipo)
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();
 
            var tipos   = await _context.TiposEquipo.OrderBy(t => t.tipo).ToListAsync();
            var estados = new List<string> { "Activo", "Devuelto", "Perdida", "Rotura", "Baja", "Mantenimiento", "Asignado" };
 
            ViewBag.Buscar       = buscar;
            ViewBag.Estado       = estado;
            ViewBag.TipoId       = tipoId;
            ViewBag.Tipos        = tipos;
            ViewBag.Estados      = estados;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
            return View(equipos);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Asignaciones).ThenInclude(a => a.Empleado)
                .Include(e => e.Asignaciones).ThenInclude(a => a.Historiales).ThenInclude(h => h.Motivo)
                .Include(e => e.ComponenteLogs.OrderByDescending(l => l.FechaHora))
                .FirstOrDefaultAsync(e => e.idEquipo == id);

            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            await CargarTipos();
            return View();
        }

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipo equipo)
        {
            ModelState.Remove("TipoEquipo");
            ModelState.Remove("Asignaciones");
            ModelState.Remove("ComponenteLogs");
            ModelState.Remove("estado_equipo");
            equipo.estado_equipo = "Activo";

            var tipo     = await _context.TiposEquipo.FindAsync(equipo.idTipoEquipo);
            var tipoUpper = tipo?.tipo?.ToUpper().Trim() ?? "";
            var esPcCompleto = tipoUpper.Contains(TipoPcCompleto);
            var esTecnico    = TiposTecnicos.Any(t => tipoUpper.Contains(t));

            // Limpiar campos que no aplican según tipo
            if (esPcCompleto)
            {
                // PC Completo: limpiar campos de equipo simple
                equipo.marca = equipo.modelo = equipo.numero_serie = null;
                equipo.Procesador = equipo.TarjetaMadre = equipo.Ram = equipo.Disco = null;
                equipo.FuenteEnergia = equipo.TarjetaGrafica = null;
                equipo.GraficosIntegrados = null;
                equipo.IMEI = null; equipo.EsInalambrico = null;
                // Gráficos PC
                if (equipo.PcCpuGraficosIntegrados == true) equipo.PcCpuTarjetaGrafica = null;
            }
            else
            {
                // Equipo simple: limpiar campos PC Completo
                equipo.PcCpuMarca = equipo.PcCpuModelo = equipo.PcCpuSerie = null;
                equipo.PcCpuProcesador = equipo.PcCpuTarjetaMadre = equipo.PcCpuRam = equipo.PcCpuDisco = null;
                equipo.PcCpuFuenteEnergia = equipo.PcCpuTarjetaGrafica = null;
                equipo.PcCpuGraficosIntegrados = null;
                equipo.PcCpuSistemaOperativo = equipo.PcCpuVersionSO = null;
                equipo.PcMonitorMarca = equipo.PcMonitorModelo = equipo.PcMonitorSerie = null;
                equipo.PcMouseMarca = equipo.PcMouseModelo = equipo.PcMouseSerie = null;
                equipo.PcMouseEsInalambrico = null;
                equipo.PcTecladoMarca = equipo.PcTecladoModelo = equipo.PcTecladoSerie = null;
                equipo.PcMousepadMarca = null;

                if (!esTecnico)
                {
                    equipo.Procesador = equipo.TarjetaMadre = equipo.Ram = equipo.Disco = null;
                    equipo.FuenteEnergia = equipo.TarjetaGrafica = null;
                    equipo.GraficosIntegrados = null;
                }
                else if (equipo.GraficosIntegrados == true)
                    equipo.TarjetaGrafica = null;
            }

            if (ModelState.IsValid)
            {
                if (!esPcCompleto && !string.IsNullOrEmpty(equipo.numero_serie) &&
                    await _context.Equipos.AnyAsync(e => e.numero_serie == equipo.numero_serie))
                {
                    ModelState.AddModelError("numero_serie", "Ya existe un equipo con ese número de serie.");
                    await CargarTipos(equipo.idTipoEquipo);
                    return View(equipo);
                }

                _context.Add(equipo);
                await _context.SaveChangesAsync();

                var desc = esPcCompleto
                    ? $"Se registró PC Completo — {tipo?.tipo}"
                    : $"Se registró el equipo {equipo.marca} {equipo.modelo} — {tipo?.tipo}";

                await _auditoriaService.RegistrarAsync("Crear", "Equipo", equipo.idEquipo, desc);
                await _notifService.NotificarAccionAsync("Creacion", "Equipo", desc, $"/Equipos/Details/{equipo.idEquipo}");

                TempData["Success"] = esPcCompleto
                    ? "PC Completo registrado correctamente."
                    : $"Equipo {equipo.marca} {equipo.modelo} registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = equipo.idEquipo });
            }

            await CargarTipos(equipo.idTipoEquipo);
            return View(equipo);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.idEquipo == id);
            if (equipo == null) return NotFound();
            await CargarTipos(equipo.idTipoEquipo);
            return View(equipo);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Equipo equipo)
        {
            if (id != equipo.idEquipo) return NotFound();
            ModelState.Remove("TipoEquipo");
            ModelState.Remove("Asignaciones");
            ModelState.Remove("ComponenteLogs");

            var tipo      = await _context.TiposEquipo.FindAsync(equipo.idTipoEquipo);
            var tipoUpper = tipo?.tipo?.ToUpper().Trim() ?? "";
            var esPcCompleto = tipoUpper.Contains(TipoPcCompleto);
            var esTecnico    = TiposTecnicos.Any(t => tipoUpper.Contains(t));

            if (esPcCompleto)
            {
                equipo.marca = equipo.modelo = equipo.numero_serie = null;
                equipo.Procesador = equipo.TarjetaMadre = equipo.Ram = equipo.Disco = null;
                equipo.FuenteEnergia = equipo.TarjetaGrafica = null;
                equipo.GraficosIntegrados = null;
                equipo.IMEI = null; equipo.EsInalambrico = null;
                if (equipo.PcCpuGraficosIntegrados == true) equipo.PcCpuTarjetaGrafica = null;
            }
            else
            {
                equipo.PcCpuMarca = equipo.PcCpuModelo = equipo.PcCpuSerie = null;
                equipo.PcCpuProcesador = equipo.PcCpuTarjetaMadre = equipo.PcCpuRam = equipo.PcCpuDisco = null;
                equipo.PcCpuFuenteEnergia = equipo.PcCpuTarjetaGrafica = null;
                equipo.PcCpuGraficosIntegrados = null;
                equipo.PcCpuSistemaOperativo = equipo.PcCpuVersionSO = null;
                equipo.PcMonitorMarca = equipo.PcMonitorModelo = equipo.PcMonitorSerie = null;
                equipo.PcMouseMarca = equipo.PcMouseModelo = equipo.PcMouseSerie = null;
                equipo.PcMouseEsInalambrico = null;
                equipo.PcTecladoMarca = equipo.PcTecladoModelo = equipo.PcTecladoSerie = null;
                equipo.PcMousepadMarca = null;

                if (!esTecnico)
                {
                    equipo.Procesador = equipo.TarjetaMadre = equipo.Ram = equipo.Disco = null;
                    equipo.FuenteEnergia = equipo.TarjetaGrafica = null;
                    equipo.GraficosIntegrados = null;
                }
                else if (equipo.GraficosIntegrados == true)
                    equipo.TarjetaGrafica = null;
            }

            if (ModelState.IsValid)
            {
                if (!esPcCompleto && !string.IsNullOrEmpty(equipo.numero_serie) &&
                    await _context.Equipos.AnyAsync(e => e.numero_serie == equipo.numero_serie && e.idEquipo != id))
                {
                    ModelState.AddModelError("numero_serie", "Ya existe otro equipo con ese número de serie.");
                    await CargarTipos(equipo.idTipoEquipo);
                    return View(equipo);
                }

                try
                {
                    var estadoActual = await _context.Equipos
                        .Where(e => e.idEquipo == id).Select(e => e.estado_equipo).FirstAsync();
                    equipo.estado_equipo = estadoActual;

                    _context.Update(equipo);
                    await _context.SaveChangesAsync();

                    var desc = esPcCompleto
                        ? $"Editó PC Completo #{id}"
                        : $"Editó equipo #{id} {equipo.marca} {equipo.modelo}";

                    await _auditoriaService.RegistrarAsync("Editar", "Equipo", id, desc);
                    await _notifService.NotificarAccionAsync("Edicion", "Equipo", desc, $"/Equipos/Details/{id}");

                    TempData["Success"] = "Equipo actualizado correctamente.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Equipos.AnyAsync(e => e.idEquipo == id)) return NotFound();
                    throw;
                }
            }

            await CargarTipos(equipo.idTipoEquipo);
            return View(equipo);
        }

        // ── CAMBIO DE COMPONENTE POST ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarComponente(
            int idEquipo, string componente, string valorNuevo, string? observaciones)
        {
            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .FirstOrDefaultAsync(e => e.idEquipo == idEquipo);
            if (equipo == null) return NotFound();

            // Obtener valor anterior del componente
            string? valorAnterior = componente switch {
                "Procesador"      => equipo.Procesador,
                "TarjetaMadre"    => equipo.TarjetaMadre,
                "Ram"             => equipo.Ram,
                "Disco"           => equipo.Disco,
                "FuenteEnergia" => equipo.FuenteEnergia,
                "TarjetaGrafica"  => equipo.TarjetaGrafica,
                "SistemaOperativo"=> equipo.sistema_operativo,
                "Version"         => equipo.version,
                _                 => null
            };

            // Actualizar campo en el equipo
            switch (componente)
            {
                case "Procesador":       equipo.Procesador       = valorNuevo; break;
                case "TarjetaMadre":     equipo.TarjetaMadre     = valorNuevo; break;
                case "Ram":              equipo.Ram              = valorNuevo; break;
                case "Disco":            equipo.Disco            = valorNuevo; break;
                case "FuenteEnergia":  equipo.FuenteEnergia  = valorNuevo; break;
                case "TarjetaGrafica":   equipo.TarjetaGrafica   = valorNuevo; break;
                case "SistemaOperativo": equipo.sistema_operativo = valorNuevo; break;
                case "Version":          equipo.version           = valorNuevo; break;
            }

            var idStr  = HttpContext.Session.GetString("UsuarioId");
            var nombre = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            var tipoEvento = componente is "SistemaOperativo" or "Version"
                ? "ActualizacionSO" : "CambioComponente";

            // Registrar en historial de componentes
            _context.EquipoComponenteLogs.Add(new EquipoComponenteLog
            {
                IdEquipo      = idEquipo,
                IdUsuario     = idUsuario,
                NombreUsuario = nombre,
                TipoEvento    = tipoEvento,
                Componente    = componente,
                ValorAnterior = valorAnterior,
                ValorNuevo    = valorNuevo,
                Observaciones = observaciones,
                FechaHora     = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("CambioComponente", "Equipo", idEquipo,
                $"Cambió {componente} de equipo #{idEquipo}: {valorAnterior} → {valorNuevo}");

            await _notifService.NotificarAccionAsync("CambioEstado", "Equipo",
                $"Cambio de {componente} en equipo {equipo.marca} {equipo.modelo}: {valorAnterior} → {valorNuevo}",
                $"/Equipos/Details/{idEquipo}");

            TempData["Success"] = $"{componente} actualizado correctamente. Cambio registrado en historial.";
            return RedirectToAction(nameof(Details), new { id = idEquipo });
        }

        // ── MANTENIMIENTO POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarMantenimiento(int idEquipo, string? observaciones)
        {
            var equipo = await _context.Equipos
                .Include(e => e.TipoEquipo)
                .Include(e => e.Asignaciones)
                .FirstOrDefaultAsync(e => e.idEquipo == idEquipo);
            if (equipo == null) return NotFound();

            var estadoAnterior = equipo.estado_equipo;
            equipo.estado_equipo = "Mantenimiento";

            // Poner asignación activa en mantenimiento
            var asignacionActiva = equipo.Asignaciones
                .FirstOrDefault(a => a.EstadoAsignacion == "Activo");
            if (asignacionActiva != null)
                asignacionActiva.EstadoAsignacion = "En mantenimiento";

            var idStr  = HttpContext.Session.GetString("UsuarioId");
            var nombre = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            // Registrar en historial de componentes
            _context.EquipoComponenteLogs.Add(new EquipoComponenteLog
            {
                IdEquipo      = idEquipo,
                IdUsuario     = idUsuario,
                NombreUsuario = nombre,
                TipoEvento    = "Mantenimiento",
                Componente    = "Estado",
                ValorAnterior = estadoAnterior,
                ValorNuevo    = "Mantenimiento",
                Observaciones = observaciones,
                FechaHora     = DateTime.Now
            });

            // Registrar en historial de asignaciones si había asignación activa
            if (asignacionActiva != null)
            {
                var motivoMante = await _context.Motivos
                    .FirstOrDefaultAsync(m => m.TipoMotivo == "Mantenimiento");
                if (motivoMante == null)
                {
                    motivoMante = new Motivo { TipoMotivo = "Mantenimiento" };
                    _context.Motivos.Add(motivoMante);
                    await _context.SaveChangesAsync();
                }

                _context.Historiales.Add(new Historial
                {
                    IdAsignacion  = asignacionActiva.IdAsignacion,
                    IdMotivo      = motivoMante.IdMotivo,
                    Fecha         = DateTime.Now,
                    Observaciones = observaciones ?? "Equipo enviado a mantenimiento"
                });
            }

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("CambioEstado", "Equipo", idEquipo,
                $"Inició mantenimiento equipo #{idEquipo} {equipo.marca} {equipo.modelo}");

            await _notifService.NotificarAccionAsync("CambioEstado", "Equipo",
                $"Equipo {equipo.marca} {equipo.modelo} en mantenimiento",
                $"/Equipos/Details/{idEquipo}");

            TempData["Success"] = "Equipo enviado a mantenimiento.";
            return RedirectToAction(nameof(Details), new { id = idEquipo });
        }

        // ── FINALIZAR MANTENIMIENTO POST ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarMantenimiento(int idEquipo, string? observaciones)
        {
            var equipo = await _context.Equipos
                .Include(e => e.Asignaciones)
                .FirstOrDefaultAsync(e => e.idEquipo == idEquipo);
            if (equipo == null) return NotFound();

            equipo.estado_equipo = "Activo";

            // Reactivar asignación si estaba en mantenimiento
            var asignacionMante = equipo.Asignaciones
                .FirstOrDefault(a => a.EstadoAsignacion == "En mantenimiento");
            if (asignacionMante != null)
            {
                asignacionMante.EstadoAsignacion = "Activo";

                var motivoFin = await _context.Motivos
                    .FirstOrDefaultAsync(m => m.TipoMotivo == "Fin mantenimiento");
                if (motivoFin == null)
                {
                    motivoFin = new Motivo { TipoMotivo = "Fin mantenimiento" };
                    _context.Motivos.Add(motivoFin);
                    await _context.SaveChangesAsync();
                }

                _context.Historiales.Add(new Historial
                {
                    IdAsignacion  = asignacionMante.IdAsignacion,
                    IdMotivo      = motivoFin.IdMotivo,
                    Fecha         = DateTime.Now,
                    Observaciones = observaciones ?? "Mantenimiento finalizado"
                });
            }

            var idStr  = HttpContext.Session.GetString("UsuarioId");
            var nombre = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            _context.EquipoComponenteLogs.Add(new EquipoComponenteLog
            {
                IdEquipo      = idEquipo,
                IdUsuario     = idUsuario,
                NombreUsuario = nombre,
                TipoEvento    = "Mantenimiento",
                Componente    = "Estado",
                ValorAnterior = "Mantenimiento",
                ValorNuevo    = "Activo",
                Observaciones = observaciones ?? "Mantenimiento finalizado",
                FechaHora     = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("CambioEstado", "Equipo", idEquipo,
                $"Finalizó mantenimiento equipo #{idEquipo}");

            await _notifService.NotificarAccionAsync("CambioEstado", "Equipo",
                $"Mantenimiento finalizado — equipo #{idEquipo} volvió a Activo",
                $"/Equipos/Details/{idEquipo}");

            TempData["Success"] = "Mantenimiento finalizado. Equipo vuelto a Activo.";
            return RedirectToAction(nameof(Details), new { id = idEquipo });
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var equipo = await _context.Equipos.Include(e => e.TipoEquipo)
                .FirstOrDefaultAsync(e => e.idEquipo == id);
            if (equipo == null) return NotFound();
            ViewBag.TotalAsignaciones = await _context.Asignaciones.CountAsync(a => a.IdEquipo == id);
            return View(equipo);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.idEquipo == id);
            if (equipo == null) return NotFound();
            try
            {
                _context.Equipos.Remove(equipo);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Eliminar", "Equipo", id,
                    $"Eliminó equipo #{id} {equipo.marca} {equipo.modelo}");

                await _notifService.NotificarAccionAsync("Eliminacion", "Equipo",
                    $"Se eliminó el equipo {equipo.marca} {equipo.modelo}");

                TempData["Success"] = "Equipo eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este equipo porque tiene asignaciones registradas.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        // ── HELPER ───────────────────────────────────────────────
        private async Task CargarTipos(int? seleccionado = null)
        {
            var tipos = await _context.TiposEquipo.OrderBy(t => t.tipo).ToListAsync();
            ViewBag.TiposList = new SelectList(tipos, "idTipoEquipo", "tipo", seleccionado);
        }
    }
}
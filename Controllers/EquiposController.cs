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

            // Filtro por rol: Oliver no ve celulares, Yane solo ve celulares
            var rolActual = HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rolActual == "SoporteTI")
                query = query.Where(e => e.TipoEquipo == null ||
                    !e.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
            else if (rolActual == "Logistica")
                query = query.Where(e => e.TipoEquipo != null &&
                    e.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
 
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
 
                // Filtrar SOLO por el tipo seleccionado (sin mezclar con PC Completo)
                query = query.Where(e => e.idTipoEquipo == tipoId);
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

            // Oliver (SoporteTI) no puede ver celulares
            var rolD = HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rolD == "SoporteTI" &&
                equipo.TipoEquipo?.tipo?.ToUpper().Contains("CELULAR") == true)
                return RedirectToAction(nameof(Index));

            ViewBag.HistorialCambios = await _context.AuditoriaLogs
            .Where(l => l.Entidad == "Equipo" && l.IdEntidad == id)
            .OrderByDescending(l => l.FechaHora)
            .Take(50)
            .ToListAsync();

            ViewBag.Bitacora = await _context.EquipoBitacoras
            .Where(b => b.IdEquipo == id)
            .OrderByDescending(b => b.Fecha)
            .ToListAsync();
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
                    // Celular sí usa sistema_operativo y version — no borrarlos
                    if (!tipoUpper.Contains("CELULAR"))
                    {
                        equipo.sistema_operativo = null;
                        equipo.version = null;
                    }
                }
                else if (equipo.GraficosIntegrados == true)
                    equipo.TarjetaGrafica = null;
            }
            var rolUsuario = HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rolUsuario == "SoporteTI" && tipoUpper.Contains("CELULAR"))
            {
                TempData["Error"] = "No tienes permiso para registrar equipos de tipo Celular.";
                await CargarTipos();
                return View(equipo);
            }
            if (rolUsuario == "Logistica")
            {
                var tipoCheck = await _context.TiposEquipo.FindAsync(equipo.idTipoEquipo);
                if (tipoCheck == null || !(tipoCheck.tipo?.ToUpper() ?? "").Contains("CELULAR"))
                {
                    TempData["Error"] = "Solo puedes registrar equipos de tipo Celular.";
                    await CargarTipos();
                    return View(equipo);
                }
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
                await _notifService.NotificarAccionAsync("Creacion", "Equipo", desc, $"/Equipos/Details/{equipo.idEquipo}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _eq1) ? _eq1 : null);

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
            // Restricción Logistica: solo puede editar Celulares
            var rolUsuario = HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rolUsuario == "Logistica")
            {
                var tipoCheck = await _context.TiposEquipo.FindAsync(equipo.idTipoEquipo);
                if (tipoCheck == null || !(tipoCheck.tipo?.ToUpper() ?? "").Contains("CELULAR"))
                {
                    TempData["Error"] = "Solo puedes editar equipos de tipo Celular.";
                    return RedirectToAction(nameof(Index));
                }
            }
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
                    // Celular sí usa sistema_operativo y version — no borrarlos
                    if (!tipoUpper.Contains("CELULAR"))
                    {
                        equipo.sistema_operativo = null;
                        equipo.version = null;
                    }
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
                    // Capturar datos ANTERIORES antes de actualizar
                    var equipoAnterior = await _context.Equipos.AsNoTracking()
                        .Include(e => e.TipoEquipo)
                        .FirstOrDefaultAsync(e => e.idEquipo == id);
 
                    var estadoActual = equipoAnterior?.estado_equipo ?? "Activo";
                    equipo.estado_equipo = estadoActual;
 
                    _context.Update(equipo);
                    await _context.SaveChangesAsync();
 
                    // Construir descripción de cambios campo por campo
                    var cambios = new List<string>();
                    if (equipoAnterior != null)
                    {
                        if (equipoAnterior.marca             != equipo.marca)             cambios.Add($"Marca: '{equipoAnterior.marca}' → '{equipo.marca}'");
                        if (equipoAnterior.modelo            != equipo.modelo)            cambios.Add($"Modelo: '{equipoAnterior.modelo}' → '{equipo.modelo}'");
                        if (equipoAnterior.numero_serie      != equipo.numero_serie)      cambios.Add($"Serie: '{equipoAnterior.numero_serie}' → '{equipo.numero_serie}'");
                        if (equipoAnterior.NombrePc          != equipo.NombrePc)          cambios.Add($"Nombre PC: '{equipoAnterior.NombrePc}' → '{equipo.NombrePc}'");
                        if (equipoAnterior.sistema_operativo != equipo.sistema_operativo) cambios.Add($"SO: '{equipoAnterior.sistema_operativo}' → '{equipo.sistema_operativo}'");
                        if (equipoAnterior.version           != equipo.version)           cambios.Add($"Versión: '{equipoAnterior.version}' → '{equipo.version}'");
                        if (equipoAnterior.Procesador        != equipo.Procesador)        cambios.Add($"Procesador: '{equipoAnterior.Procesador}' → '{equipo.Procesador}'");
                        if (equipoAnterior.Ram               != equipo.Ram)               cambios.Add($"RAM: '{equipoAnterior.Ram}' → '{equipo.Ram}'");
                        if (equipoAnterior.Disco             != equipo.Disco)             cambios.Add($"Disco: '{equipoAnterior.Disco}' → '{equipo.Disco}'");
                        if (equipoAnterior.PcCpuSistemaOperativo != equipo.PcCpuSistemaOperativo) cambios.Add($"SO (PC): '{equipoAnterior.PcCpuSistemaOperativo}' → '{equipo.PcCpuSistemaOperativo}'");
                        if (equipoAnterior.PcCpuVersionSO        != equipo.PcCpuVersionSO)        cambios.Add($"Versión SO (PC): '{equipoAnterior.PcCpuVersionSO}' → '{equipo.PcCpuVersionSO}'");
                        if (equipoAnterior.Observaciones     != equipo.Observaciones)     cambios.Add($"Observaciones actualizadas");
                        if (equipoAnterior.idTipoEquipo      != equipo.idTipoEquipo)      cambios.Add($"Tipo cambiado");
                    }
 
                    var desc = esPcCompleto
                        ? $"Editó PC Completo '{equipo.NombrePc ?? "sin nombre"}' (#{id})"
                        : $"Editó equipo #{id} {equipo.marca} {equipo.modelo}";
 
                    var datosAnteriores = cambios.Any()
                        ? string.Join(" | ", cambios)
                        : "Sin cambios detectados";
 
                    await _auditoriaService.RegistrarAsync("Editar", "Equipo", id, desc, datosAnteriores);
                    await _notifService.NotificarAccionAsync("Edicion", "Equipo", desc, $"/Equipos/Details/{id}",
                        idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _eq2) ? _eq2 : null);

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
                $"/Equipos/Details/{idEquipo}",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _eq3) ? _eq3 : null);

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
                $"/Equipos/Details/{idEquipo}",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _eq4) ? _eq4 : null);

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

            // Reactivar asignación si estaba en mantenimiento
            var asignacionMante = equipo.Asignaciones
                .FirstOrDefault(a => a.EstadoAsignacion == "En mantenimiento");
            if (asignacionMante != null)
            {
                asignacionMante.EstadoAsignacion = "Activo";
                equipo.estado_equipo = "Asignado"; // tenía asignación → vuelve a Asignado

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
            else
            {
                equipo.estado_equipo = "Activo"; // no tenía asignación → vuelve a Activo
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
                ValorNuevo    = asignacionMante != null ? "Asignado" : "Activo",
                Observaciones = observaciones ?? "Mantenimiento finalizado",
                FechaHora     = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("CambioEstado", "Equipo", idEquipo,
                $"Finalizó mantenimiento equipo #{idEquipo}");

            await _notifService.NotificarAccionAsync("CambioEstado", "Equipo",
                $"Mantenimiento finalizado — equipo #{idEquipo} volvió a Activo",
                $"/Equipos/Details/{idEquipo}",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _eq5) ? _eq5 : null);

            TempData["Success"] = asignacionMante != null ? "Mantenimiento finalizado. Equipo vuelto a Asignado." : "Mantenimiento finalizado. Equipo vuelto a Activo.";
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
        public async Task<IActionResult> ExportarExcel(string? buscar, string? estado, int? tipoId)
        {
            var query = _context.Equipos.Include(e => e.TipoEquipo).AsQueryable();
        
            var rolActual = HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rolActual == "SoporteTI")
                query = query.Where(e => e.TipoEquipo == null || !e.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
            else if (rolActual == "Logistica")
                query = query.Where(e => e.TipoEquipo != null && e.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
        
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.marca        != null && e.marca.Contains(buscar))        ||
                    (e.modelo       != null && e.modelo.Contains(buscar))       ||
                    (e.numero_serie != null && e.numero_serie.Contains(buscar)) ||
                    (e.NombrePc     != null && e.NombrePc.Contains(buscar)));
        
            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado == "otros")
                    query = query.Where(e => e.estado_equipo != "Activo" && e.estado_equipo != "Asignado" && e.estado_equipo != "Mantenimiento");
                else
                    query = query.Where(e => e.estado_equipo == estado);
            }
        
            if (tipoId.HasValue)
                query = query.Where(e => e.idTipoEquipo == tipoId);
        
            var equipos = await query.OrderByDescending(e => e.idEquipo).ToListAsync();
        
            var columnas = new List<string> { "Tipo", "Nombre / Marca", "Modelo", "N° Serie", "S.O.", "Versión", "Estado", "Fecha Compra" };
            var filas = equipos.Select(e => {
                var esPc = e.TipoEquipo?.tipo?.ToUpper().Contains("PC COMPLETO") == true;
                return new List<string> {
                    e.TipoEquipo?.tipo ?? "—",
                    esPc ? (e.NombrePc ?? "Sin nombre") : ((e.marca ?? "—") + " " + (e.modelo ?? "")),
                    esPc ? (e.PcCpuModelo ?? "—") : (e.modelo ?? "—"),
                    esPc ? (e.PcCpuSerie ?? "—") : (e.numero_serie ?? "—"),
                    esPc ? (e.PcCpuSistemaOperativo ?? "—") : (e.sistema_operativo ?? "—"),
                    esPc ? (e.PcCpuVersionSO ?? "—") : (e.version ?? "—"),
                    e.estado_equipo ?? "—",
                    e.fecha_compra.ToString("dd/MM/yyyy")
                };
            }).ToList();
        
            return GenerarCsv(columnas, filas, "Equipos_TI");
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
        public async Task<IActionResult> ExportarPdf(string? buscar, string? estado, int? tipoId)
        {
            var query = _context.Equipos.Include(e => e.TipoEquipo).AsQueryable();
        
            var rolActual = HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rolActual == "SoporteTI")
                query = query.Where(e => e.TipoEquipo == null || !e.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
            else if (rolActual == "Logistica")
                query = query.Where(e => e.TipoEquipo != null && e.TipoEquipo.tipo.ToUpper().Contains("CELULAR"));
        
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.marca        != null && e.marca.Contains(buscar))        ||
                    (e.modelo       != null && e.modelo.Contains(buscar))       ||
                    (e.numero_serie != null && e.numero_serie.Contains(buscar)) ||
                    (e.NombrePc     != null && e.NombrePc.Contains(buscar)));
        
            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado == "otros")
                    query = query.Where(e => e.estado_equipo != "Activo" && e.estado_equipo != "Asignado" && e.estado_equipo != "Mantenimiento");
                else
                    query = query.Where(e => e.estado_equipo == estado);
            }
            if (tipoId.HasValue)
                query = query.Where(e => e.idTipoEquipo == tipoId);
        
            var equipos = await query.OrderByDescending(e => e.idEquipo).ToListAsync();
        
            var columnas = new List<string> { "Tipo", "Nombre / Marca", "N° Serie", "S.O.", "Estado", "F. Compra" };
            var filas = equipos.Select(e => {
                var esPc = e.TipoEquipo?.tipo?.ToUpper().Contains("PC COMPLETO") == true;
                return new List<string> {
                    e.TipoEquipo?.tipo ?? "—",
                    esPc ? (e.NombrePc ?? "Sin nombre") : ((e.marca ?? "—") + " " + (e.modelo ?? "")),
                    esPc ? (e.PcCpuSerie ?? "—") : (e.numero_serie ?? "—"),
                    esPc ? (e.PcCpuSistemaOperativo ?? "—") : (e.sistema_operativo ?? "—"),
                    e.estado_equipo ?? "—",
                    e.fecha_compra.ToString("dd/MM/yyyy")
                };
            }).ToList();
        
            return GenerarPdf("Equipos TI", columnas, filas);
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
                    $"Se eliminó el equipo {equipo.marca} {equipo.modelo}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _eq6) ? _eq6 : null);

                TempData["Success"] = "Equipo eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este equipo porque tiene asignaciones registradas.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }
        // ════════════════════════════════════════════════════════════════
        // BITÁCORA DE EQUIPOS — agregar dentro de EquiposController
        // Requiere: _context.EquipoBitacoras (DbSet<EquipoBitacora>)
        // ════════════════════════════════════════════════════════════════

        // ── POST: Registrar evento en Bitácora ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarBitacora(
            int idEquipo,
            string estadoNuevo,
            string motivo,
            DateTime fecha,
            bool esProgramado = false,
            string? observaciones = null)
        {
            var equipo = await _context.Equipos.FindAsync(idEquipo);
            if (equipo == null) return NotFound();

            var estadoAnterior = equipo.estado_equipo;

            // Actualizar estado del equipo solo si NO es programado (futuro)
            if (!esProgramado || fecha.Date <= DateTime.Today)
            {
                equipo.estado_equipo = estadoNuevo;
            }

            var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema";
            var idStr         = HttpContext.Session.GetString("UsuarioId");
            int? idUsuario    = int.TryParse(idStr, out int uid) ? uid : null;

            _context.EquipoBitacoras.Add(new EquipoBitacora
            {
                IdEquipo       = idEquipo,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo    = estadoNuevo,
                Motivo         = motivo.Trim(),
                Fecha          = fecha,
                EsProgramado   = esProgramado,
                Completado     = false,
                RegistradoPor  = nombreUsuario,
                IdUsuario      = idUsuario,
                FechaRegistro  = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Editar", "Equipo", idEquipo,
                esProgramado
                    ? $"Mantenimiento programado para {fecha:dd/MM/yyyy} — {motivo}"
                    : $"Estado cambiado de '{estadoAnterior}' a '{estadoNuevo}' — {motivo}");

            TempData["Success"] = esProgramado
                ? $"Mantenimiento programado para el {fecha:dd/MM/yyyy}."
                : "Evento registrado en la Bitácora.";

            return RedirectToAction(nameof(Details), new { id = idEquipo });
        }

        // ── POST: Marcar mantenimiento programado como completado ─
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletarMantenimiento(int idBitacora)
        {
            var bitacora = await _context.EquipoBitacoras
                .Include(b => b.Equipo)
                .FirstOrDefaultAsync(b => b.IdBitacora == idBitacora);

            if (bitacora == null) return NotFound();

            bitacora.Completado      = true;
            bitacora.EsProgramado    = false;

            // Actualizar estado del equipo al estado registrado
            if (bitacora.Equipo != null)
                bitacora.Equipo.estado_equipo = bitacora.EstadoNuevo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Mantenimiento marcado como completado.";
            return RedirectToAction(nameof(Details), new { id = bitacora.IdEquipo });
        }

        // ── HELPER ───────────────────────────────────────────────
        private async Task CargarTipos(int? seleccionado = null)
        {
            var tipos = await _context.TiposEquipo.OrderBy(t => t.tipo).ToListAsync();
            ViewBag.TiposList = new SelectList(tipos, "idTipoEquipo", "tipo", seleccionado);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class CarrosController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public CarrosController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estado, string? categoria, int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.Carros
                .Include(c => c.EmpleadosCarros).ThenInclude(ec => ec.Empleado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c =>
                    c.Placa.Contains(buscar) || c.Marca.Contains(buscar) || c.Modelo.Contains(buscar) ||
                    (c.NumeroMotor != null && c.NumeroMotor.Contains(buscar)) ||
                    c.EmpleadosCarros.Any(ec =>
                        (ec.Empleado.nombre != null && ec.Empleado.nombre.Contains(buscar)) ||
                        (ec.Empleado.paterno != null && ec.Empleado.paterno.Contains(buscar))));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(c => c.Estado == estado);

            if (!string.IsNullOrWhiteSpace(categoria))
                query = query.Where(c => c.Categoria == categoria);

            int total = await query.CountAsync();
            var carros = await query
                .OrderByDescending(c => c.IdCarro)
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

            var categorias = await _context.Carros
                .Where(c => c.Categoria != null).Select(c => c.Categoria!)
                .Distinct().OrderBy(c => c).ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Estado       = estado;
            ViewBag.Categoria    = categoria;
            ViewBag.Categorias   = categorias;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
            return View(carros);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var carro = await _context.Carros
                .Include(c => c.CarroSeguros).ThenInclude(cs => cs.Seguro)
                .Include(c => c.MantenimientosCarros).ThenInclude(m => m.TipoMantenimiento)
                .Include(c => c.CarroAsesorios).ThenInclude(ca => ca.Asesorio)
                .Include(c => c.CarroModalidades).ThenInclude(cm => cm.Modalidad)
                .Include(c => c.EmpleadosCarros).ThenInclude(ec => ec.Empleado)
                .FirstOrDefaultAsync(c => c.IdCarro == id);

            if (carro == null) return NotFound();

            // Historial de estados
            var estadoLog = await _context.CarroEstadoLogs
                .Where(l => l.IdCarro == id)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();
            ViewBag.EstadoLog = estadoLog;

            // Historial de conductores
            var conductorLog = await _context.CarroConductorLogs
                .Where(l => l.IdCarro == id)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();
            ViewBag.ConductorLog = conductorLog;

            // Lista de empleados activos para el select de conductor
            var empleados = await _context.Empleados
                .Where(e => e.estado == "Activo")
                .OrderBy(e => e.paterno)
                .Select(e => new {
                    e.idEmpleado,
                    NombreCompleto = e.nombre + " " + e.paterno + " " + e.materno
                })
                .ToListAsync();
            ViewBag.EmpleadosList = new SelectList(empleados, "idEmpleado", "NombreCompleto");
            ViewBag.HistorialCambios = await _context.AuditoriaLogs
            .Where(l => l.Entidad == "Carro" && l.IdEntidad == id)
            .OrderByDescending(l => l.FechaHora)
            .Take(50)
            .ToListAsync();

            // Historial de cambios de modalidad
            ViewBag.ModalidadLog = await _context.CarroModalidadLogs
                .Where(l => l.IdCarro == id)
                .OrderByDescending(l => l.FechaRegistro)
                .ToListAsync();
            return View(carro);
        }

        // ── ASIGNAR CONDUCTOR POST ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarConductor(int idCarro, int? idEmpleado, bool sinConductor = false)
        {
            // 1. Obtener conductor ANTERIOR antes de borrar
            var actual = await _context.EmpleadosCarros
                .Include(ec => ec.Empleado)
                .FirstOrDefaultAsync(ec => ec.IdCarro == idCarro);

            int?    idAnterior      = actual?.IdEmpleado;
            string? nombreAnterior  = actual?.Empleado != null
                ? $"{actual.Empleado.nombre} {actual.Empleado.paterno} {actual.Empleado.materno}".Trim()
                : null;

            // 2. Remover conductor actual
            var actuales = await _context.EmpleadosCarros
                .Where(ec => ec.IdCarro == idCarro).ToListAsync();
            _context.EmpleadosCarros.RemoveRange(actuales);

            // 3. Asignar nuevo conductor (si no se pulsó "Sin conductor")
            int?    idNuevo      = null;
            string? nombreNuevo  = null;

            if (!sinConductor && idEmpleado.HasValue && idEmpleado.Value > 0)
            {
                var empleado = await _context.Empleados.FindAsync(idEmpleado.Value);
                idNuevo     = idEmpleado.Value;
                nombreNuevo = empleado != null
                    ? $"{empleado.nombre} {empleado.paterno} {empleado.materno}".Trim()
                    : null;

                _context.EmpleadosCarros.Add(new EmpleadoCarro
                {
                    IdCarro    = idCarro,
                    IdEmpleado = idEmpleado.Value
                });
            }

            // 4. Registrar en CarroConductorLog SOLO si hubo cambio real
            bool huboCambio = idAnterior != idNuevo;
            if (huboCambio)
            {
                var idStr      = HttpContext.Session.GetString("UsuarioId");
                var nombre     = HttpContext.Session.GetString("UsuarioNombre");
                int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

                _context.CarroConductorLogs.Add(new CarroConductorLog
                {
                    IdCarro                 = idCarro,
                    IdUsuario               = idUsuario,
                    NombreUsuario           = nombre,
                    IdEmpleadoAnterior      = idAnterior,
                    NombreConductorAnterior = nombreAnterior,
                    IdEmpleadoNuevo         = idNuevo,
                    NombreConductorNuevo    = nombreNuevo,
                    FechaHora               = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Editar", "Carro", idCarro,
                idNuevo.HasValue
                    ? $"Asignó conductor '{nombreNuevo}' al vehículo #{idCarro}"
                    : $"Removió conductor del vehículo #{idCarro}");

            await _notifService.NotificarAccionAsync(
                idNuevo.HasValue ? "Creacion" : "Eliminacion",
                "Conductor",
                idNuevo.HasValue ? "Conductor asignado a vehículo" : "Conductor removido del vehículo",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _ca0) ? _ca0 : null);

            TempData["Success"] = idNuevo.HasValue
                ? $"Conductor asignado: {nombreNuevo}."
                : "Conductor removido del vehículo.";

            return RedirectToAction(nameof(Details), new { id = idCarro });
        }

        // ── CAMBIAR ESTADO POST ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int idCarro, string nuevoEstado, string motivo, string? observaciones)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var estadosPermitidos = new[] { "Activo", "Inactivo" };
            if (!estadosPermitidos.Contains(nuevoEstado))
            {
                TempData["Error"] = "Estado no permitido desde este panel.";
                return RedirectToAction(nameof(Details), new { id = idCarro });
            }

            var estadoAnterior = carro.Estado;
            carro.Estado = nuevoEstado;

            var idStr      = HttpContext.Session.GetString("UsuarioId");
            var nombre     = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            _context.CarroEstadoLogs.Add(new CarroEstadoLog
            {
                IdCarro        = idCarro,
                IdUsuario      = idUsuario,
                NombreUsuario  = nombre,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo    = nuevoEstado,
                Motivo         = motivo,
                Observaciones  = observaciones,
                FechaHora      = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("CambioEstado", "Carro", idCarro,
                $"Cambió estado vehículo #{idCarro} de {estadoAnterior} → {nuevoEstado}. Motivo: {motivo}");

            TempData["Success"] = $"Estado cambiado a \"{nuevoEstado}\". Registrado en historial.";
            return RedirectToAction(nameof(Details), new { id = idCarro });
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Carro carro)
        {
            ModelState.Remove("EmpleadosCarros");
            ModelState.Remove("CarroSeguros");
            ModelState.Remove("CarroAsesorios");
            ModelState.Remove("CarroModalidades");
            ModelState.Remove("MantenimientosCarros");
            ModelState.Remove("Estado");

            carro.Estado = "Activo";

            // Convertir a mayúsculas
            carro.Placa        = carro.Placa?.ToUpper().Trim();
            carro.Marca        = carro.Marca?.ToUpper().Trim();
            carro.Modelo       = carro.Modelo?.ToUpper().Trim();
            carro.NumeroMotor  = carro.NumeroMotor?.ToUpper().Trim();

            if (ModelState.IsValid)
            {
                if (await _context.Carros.AnyAsync(c => c.Placa == carro.Placa))
                {
                    ModelState.AddModelError("Placa", "Ya existe un vehículo con esa placa.");
                    return View(carro);
                }

                _context.Add(carro);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Crear", "Carro", carro.IdCarro,
                    $"Registró vehículo {carro.Placa} — {carro.Marca} {carro.Modelo}");

                await _notifService.NotificarAccionAsync("Creacion", "Carro",
                    $"Registró vehículo {carro.Placa} — {carro.Marca} {carro.Modelo}",
                    $"/Carros/Details/{carro.IdCarro}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _ca1) ? _ca1 : null);
                TempData["Success"] = $"Vehículo {carro.Placa} registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = carro.IdCarro });
            }
            return View(carro);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            return View(carro);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Carro carro)
        {
            if (id != carro.IdCarro) return NotFound();
            ModelState.Remove("EmpleadosCarros");
            ModelState.Remove("CarroSeguros");
            ModelState.Remove("CarroAsesorios");
            ModelState.Remove("CarroModalidades");
            ModelState.Remove("MantenimientosCarros");
            ModelState.Remove("Estado");

            // Convertir a mayúsculas
            carro.Placa        = carro.Placa?.ToUpper().Trim();
            carro.Marca        = carro.Marca?.ToUpper().Trim();
            carro.Modelo       = carro.Modelo?.ToUpper().Trim();
            carro.NumeroMotor  = carro.NumeroMotor?.ToUpper().Trim();

            if (ModelState.IsValid)
            {
                if (await _context.Carros.AnyAsync(c => c.Placa == carro.Placa && c.IdCarro != id))
                {
                    ModelState.AddModelError("Placa", "Ya existe otro vehículo con esa placa.");
                    return View(carro);
                }

                try
                {
                    var estadoActual = await _context.Carros
                        .Where(c => c.IdCarro == id).Select(c => c.Estado).FirstAsync();
                    carro.Estado = estadoActual;

                    _context.Update(carro);
                    await _context.SaveChangesAsync();

                    var carroAnterior = await _context.Carros.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.IdCarro == id);
                    var cambiosCarro = new List<string>();
                    if (carroAnterior != null)
                    {
                        if (carroAnterior.Placa   != carro.Placa)   cambiosCarro.Add($"Placa: '{carroAnterior.Placa}' → '{carro.Placa}'");
                        if (carroAnterior.Marca   != carro.Marca)   cambiosCarro.Add($"Marca: '{carroAnterior.Marca}' → '{carro.Marca}'");
                        if (carroAnterior.Modelo  != carro.Modelo)  cambiosCarro.Add($"Modelo: '{carroAnterior.Modelo}' → '{carro.Modelo}'");
                        if (carroAnterior.Estado  != carro.Estado)  cambiosCarro.Add($"Estado: '{carroAnterior.Estado}' → '{carro.Estado}'");
                    }
                    var datosCarroAnt = cambiosCarro.Any() ? string.Join(" | ", cambiosCarro) : null;
                    await _auditoriaService.RegistrarAsync("Editar", "Carro", id,
                        $"Editó vehículo {carro.Placa}", datosCarroAnt);

                    await _notifService.NotificarAccionAsync("Edicion", "Carro",
                        $"Editó vehículo {carro.Placa}",
                        $"/Carros/Details/{id}",
                        idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _ca2) ? _ca2 : null);

                    TempData["Success"] = $"Vehículo {carro.Placa} actualizado.";
                    return RedirectToAction(nameof(Details), new { id = carro.IdCarro });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Carros.AnyAsync(c => c.IdCarro == id)) return NotFound();
                    throw;
                }
            }
            return View(carro);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            return View(carro);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            try
            {
                _context.Carros.Remove(carro);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Eliminar", "Carro", id,
                    $"Eliminó vehículo {carro.Placa}");

                await _notifService.NotificarAccionAsync("Eliminacion", "Carro",
                    $"Eliminó vehículo {carro.Placa}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _ca3) ? _ca3 : null);

                TempData["Success"] = $"Vehículo {carro.Placa} eliminado.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: tiene registros asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
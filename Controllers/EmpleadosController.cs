using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
     // ── ViewModel para Create y Edit ─────────────────────────────
    public class EmpleadoFormViewModel
    {
        public Empleado Empleado { get; set; } = new();

        // Lista de todos los grupos con un checkbox por cada uno
        public List<GrupoCheckbox> Grupos { get; set; } = new();
    }

    public class GrupoCheckbox
    {
        public int    IdGrupo   { get; set; }
        public string Area      { get; set; } = string.Empty;
        public bool   Marcado   { get; set; }   // true = ya pertenece a este grupo
    }
    public class EmpleadosController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public EmpleadosController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estado, string? orden = "az", int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Empleados.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.nombre    != null && e.nombre.Contains(buscar))  ||
                    (e.paterno   != null && e.paterno.Contains(buscar)) ||
                    (e.dni       != null && e.dni.Contains(buscar))     ||
                    (e.correo    != null && e.correo.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(e => e.estado == estado);

            int total = await query.CountAsync();

            var empleados = await query.OrderByDescending(e => e.idEmpleado)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar        = buscar;
            ViewBag.Estado        = estado;
            ViewBag.Orden         = orden;
            ViewBag.Pagina        = pagina;
            ViewBag.Total         = total;
            ViewBag.PorPagina     = porPagina;
            ViewBag.TotalPaginas  = (int)Math.Ceiling((double)total / porPagina);

            return View(empleados);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.idEmpleado == id);

            if (empleado == null) return NotFound();

            var grupos = await _context.EmpleadoGrupos
                .Include(eg => eg.Grupo)
                .Where(eg => eg.IdEmpleado == id)
                .Select(eg => eg.Grupo)
                .ToListAsync();

            var seguros = await _context.EmpleadoSeguros
                .Include(es => es.Seguro)
                .Where(es => es.IdEmpleado == id)
                .ToListAsync();

            var estadoLog = await _context.EmpleadoEstadoLogs
                .Where(l => l.IdEmpleado == id)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();

            ViewBag.Grupos    = grupos;
            ViewBag.Seguros   = seguros;
            ViewBag.EstadoLog = estadoLog;
            ViewBag.HistorialCambios = await _context.AuditoriaLogs
            .Where(l => l.Entidad == "Empleado" && l.IdEntidad == id)
            .OrderByDescending(l => l.FechaHora)
            .Take(50)
            .ToListAsync();
            return View(empleado);
        }

       
        // ── CREATE GET ───────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            var vm = new EmpleadoFormViewModel
            {
                Grupos = await _context.Grupos
                    .OrderBy(g => g.area)
                    .Select(g => new GrupoCheckbox
                    {
                        IdGrupo = g.idGrupo,
                        Area    = g.area ?? "",
                        Marcado = false
                    })
                    .ToListAsync()
            };
            return View(vm);
        }

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmpleadoFormViewModel vm)
        {
            // Quitar validaciones de Grupos (vienen como lista auxiliar)
            ModelState.Remove("Grupos");
            ModelState.Remove("Empleado.estado");
            vm.Empleado.estado = "Activo";

            if (string.IsNullOrEmpty(vm.Empleado.TipoDocumento))
                vm.Empleado.TipoDocumento = "DNI";

            if (ModelState.IsValid)
            {
                bool dniExiste = await _context.Empleados
                    .AnyAsync(e => e.dni == vm.Empleado.dni);

                if (dniExiste)
                {
                    ModelState.AddModelError("Empleado.dni", "Ya existe un empleado con ese DNI.");
                    vm.Grupos = await CargarGrupos();
                    return View(vm);
                }

                _context.Empleados.Add(vm.Empleado);
                await _context.SaveChangesAsync();

                // Guardar grupos marcados
                foreach (var g in vm.Grupos.Where(g => g.Marcado))
                {
                    _context.EmpleadoGrupos.Add(new EmpleadoGrupo
                    {
                        IdEmpleado = vm.Empleado.idEmpleado,
                        IdGrupo    = g.IdGrupo
                    });
                }
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Crear", "Empleado", vm.Empleado.idEmpleado,
                    $"Registró empleado {vm.Empleado.nombre} {vm.Empleado.paterno} (DNI: {vm.Empleado.dni})");
                
                await _notifService.NotificarAccionAsync("Creacion", "Empleado",
                    $"Registró empleado {vm.Empleado.nombre} {vm.Empleado.paterno} (DNI: {vm.Empleado.dni})",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _e1) ? _e1 : null);
                TempData["Success"] = $"Empleado {vm.Empleado.nombre} {vm.Empleado.paterno} registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = vm.Empleado.idEmpleado });
            }

            vm.Grupos = await CargarGrupos();
            return View(vm);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.idEmpleado == id);

            if (empleado == null) return NotFound();

            // Ids de grupos que ya tiene
            var idsActuales = await _context.EmpleadoGrupos
                .Where(eg => eg.IdEmpleado == id)
                .Select(eg => eg.IdGrupo)
                .ToListAsync();

            var vm = new EmpleadoFormViewModel
            {
                Empleado = empleado,
                Grupos   = await _context.Grupos
                    .OrderBy(g => g.area)
                    .Select(g => new GrupoCheckbox
                    {
                        IdGrupo = g.idGrupo,
                        Area    = g.area ?? "",
                        Marcado = idsActuales.Contains(g.idGrupo)  // preselecciona los que ya tiene
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmpleadoFormViewModel vm)
        {
            if (id != vm.Empleado.idEmpleado) return NotFound();

            ModelState.Remove("Grupos");
            ModelState.Remove("Empleado.estado");

            // Recuperar estado actual de BD para no pisarlo al editar
            var estadoActual = await _context.Empleados
                .AsNoTracking()
                .Where(e => e.idEmpleado == id)
                .Select(e => e.estado)
                .FirstOrDefaultAsync();
            vm.Empleado.estado = estadoActual ?? "Activo";

            if (ModelState.IsValid)
            {
                bool dniExiste = await _context.Empleados
                    .AnyAsync(e => e.dni == vm.Empleado.dni && e.idEmpleado != id);

                if (dniExiste)
                {
                    ModelState.AddModelError("Empleado.dni", "Ya existe otro empleado con ese DNI.");
                    vm.Grupos = await CargarGrupos(id);
                    return View(vm);
                }

                try
                {
                    _context.Update(vm.Empleado);

                    // Reemplazar grupos: borrar todos los actuales y guardar los nuevos
                    var relacionesActuales = await _context.EmpleadoGrupos
                        .Where(eg => eg.IdEmpleado == id)
                        .ToListAsync();

                    _context.EmpleadoGrupos.RemoveRange(relacionesActuales);

                    foreach (var g in vm.Grupos.Where(g => g.Marcado))
                    {
                        _context.EmpleadoGrupos.Add(new EmpleadoGrupo
                        {
                            IdEmpleado = id,
                            IdGrupo    = g.IdGrupo
                        });
                    }

                    await _context.SaveChangesAsync();
                    var empAnterior = await _context.Empleados.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.idEmpleado == id);
                    var cambiosEmp = new List<string>();
                    if (empAnterior != null)
                    {
                        if (empAnterior.nombre     != vm.Empleado.nombre)     cambiosEmp.Add($"Nombre: '{empAnterior.nombre}' → '{vm.Empleado.nombre}'");
                        if (empAnterior.paterno    != vm.Empleado.paterno)    cambiosEmp.Add($"Apellido: '{empAnterior.paterno}' → '{vm.Empleado.paterno}'");
                        if (empAnterior.materno    != vm.Empleado.materno)    cambiosEmp.Add($"Apellido materno: '{empAnterior.materno}' → '{vm.Empleado.materno}'");
                        if (empAnterior.dni        != vm.Empleado.dni)        cambiosEmp.Add($"DNI: '{empAnterior.dni}' → '{vm.Empleado.dni}'");
                        if (empAnterior.correo     != vm.Empleado.correo)     cambiosEmp.Add($"Correo: '{empAnterior.correo ?? "—"}' → '{vm.Empleado.correo ?? "—"}'");
                        if (empAnterior.direccion  != vm.Empleado.direccion)  cambiosEmp.Add($"Dirección actualizada");
                        if (empAnterior.estado     != vm.Empleado.estado)     cambiosEmp.Add($"Estado: '{empAnterior.estado}' → '{vm.Empleado.estado}'");
                    }
                    var datosEmpAnt = cambiosEmp.Any() ? string.Join(" | ", cambiosEmp) : null;
                    await _auditoriaService.RegistrarAsync("Editar", "Empleado", id,
                        $"Editó empleado {vm.Empleado.nombre} {vm.Empleado.paterno}", datosEmpAnt);
                    
                await _notifService.NotificarAccionAsync("Edicion", "Empleado",
                    $"Editó empleado {vm.Empleado.nombre} {vm.Empleado.paterno}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _e2) ? _e2 : null);
                TempData["Success"] = $"Empleado {vm.Empleado.nombre} {vm.Empleado.paterno} actualizado correctamente.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Empleados.AnyAsync(e => e.idEmpleado == id))
                        return NotFound();
                    throw;
                }
            }

            vm.Grupos = await CargarGrupos(id);
            return View(vm);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.idEmpleado == id);

            if (empleado == null) return NotFound();
            return View(empleado);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.idEmpleado == id);

            if (empleado == null) return NotFound();

            try
            {
                // Borrar relaciones de grupo primero
                var relaciones = await _context.EmpleadoGrupos
                    .Where(eg => eg.IdEmpleado == id).ToListAsync();
                _context.EmpleadoGrupos.RemoveRange(relaciones);

                _context.Empleados.Remove(empleado);
                await _context.SaveChangesAsync();
                await _auditoriaService.RegistrarAsync("Eliminar", "Empleado", id,
                    $"Eliminó empleado {empleado.nombre} {empleado.paterno}");
                
                await _notifService.NotificarAccionAsync("Eliminacion", "Empleado",
                    $"Eliminó empleado {empleado.nombre} {empleado.paterno}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _e3) ? _e3 : null);
                TempData["Success"] = $"Empleado {empleado.nombre} {empleado.paterno} eliminado.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este empleado porque tiene registros asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // ── Helper privado ────────────────────────────────────────
        private async Task<List<GrupoCheckbox>> CargarGrupos(int? idEmpleado = null)
        {
            var idsActuales = idEmpleado.HasValue
                ? await _context.EmpleadoGrupos
                    .Where(eg => eg.IdEmpleado == idEmpleado)
                    .Select(eg => eg.IdGrupo)
                    .ToListAsync()
                : new List<int>();

            return await _context.Grupos
                .OrderBy(g => g.area)
                .Select(g => new GrupoCheckbox
                {
                    IdGrupo = g.idGrupo,
                    Area    = g.area ?? "",
                    Marcado = idsActuales.Contains(g.idGrupo)
                })
                .ToListAsync();
        }
        // ── CAMBIAR ESTADO EMPLEADO POST ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int idEmpleado, string nuevoEstado, string? observaciones)
        {
            var empleado = await _context.Empleados.FindAsync(idEmpleado);
            if (empleado == null) return NotFound();

            var estadoAnterior = empleado.estado ?? "Activo";
            empleado.estado    = nuevoEstado;

            var idStr  = HttpContext.Session.GetString("UsuarioId");
            var nombre = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            _context.EmpleadoEstadoLogs.Add(new EmpleadoEstadoLog
            {
                IdEmpleado     = idEmpleado,
                IdUsuario      = idUsuario,
                NombreUsuario  = nombre,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo    = nuevoEstado,
                Observaciones  = observaciones,
                FechaHora      = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("CambioEstado", "Empleado", idEmpleado,
                $"Cambió estado empleado #{idEmpleado} de {estadoAnterior} → {nuevoEstado}");

           await _notifService.NotificarAccionAsync("CambioEstado", "Empleado",
                $"Estado de empleado cambió a {nuevoEstado}",
                $"/Empleados/Details/{idEmpleado}",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _e4) ? _e4 : null);

            TempData["Success"] = $"Estado cambiado a '{nuevoEstado}'. Registrado en historial.";
            return RedirectToAction(nameof(Details), new { id = idEmpleado });
        }
    }
}
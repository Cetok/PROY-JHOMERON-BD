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

            var empleados = await (orden == "za"
                ? query.OrderByDescending(e => e.paterno)
                : query.OrderBy(e => e.paterno))
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

            ViewBag.Grupos = grupos;
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
                
                await _notifService.NotificarAccionAsync("Creacion", "Empleado", $"Registró empleado {vm.Empleado.nombre} {vm.Empleado.paterno} (DNI: {vm.Empleado.dni})");
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
                    await _auditoriaService.RegistrarAsync("Editar", "Empleado", id,
                        $"Editó empleado {vm.Empleado.nombre} {vm.Empleado.paterno}");
                    
                await _notifService.NotificarAccionAsync("Edicion", "Empleado", $"Editó empleado {vm.Empleado.nombre} {vm.Empleado.paterno}");
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
                
                await _notifService.NotificarAccionAsync("Eliminacion", "Empleado", $"Eliminó empleado {empleado.nombre} {empleado.paterno}");
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
    }
}
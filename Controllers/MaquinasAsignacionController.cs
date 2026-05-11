using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

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
        public async Task<IActionResult> Index(string? buscar, string? estadoOp, int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.MaquinaAsignaciones
                .Include(a => a.Maquina)
                .Include(a => a.Grupo)
                .Include(a => a.Encargados).ThenInclude(e => e.Empleado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a =>
                    a.Maquina.NumeroMaquina.Contains(buscar) ||
                    a.Maquina.NombreMaquina.Contains(buscar) ||
                    (a.Grupo.area != null && a.Grupo.area.Contains(buscar)) ||
                    a.Encargados.Any(e =>
                        (e.Empleado.nombre != null && e.Empleado.nombre.Contains(buscar)) ||
                        (e.Empleado.paterno != null && e.Empleado.paterno.Contains(buscar))));

            if (!string.IsNullOrWhiteSpace(estadoOp))
                query = query.Where(a => a.EstadoOperativo == estadoOp);

            int total        = await query.CountAsync();
            var asignaciones = await query.OrderByDescending(a => a.FechaAsignacion)
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.EstadoOp     = estadoOp;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
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
            await CargarSelectLists(idMaquina);
            var vm = new MaquinaAsignacion
            {
                FechaAsignacion = DateTime.Today,
                IdMaquina       = idMaquina ?? 0
            };
            return View(vm);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaquinaAsignacion asig, List<int> idsEncargados)
        {
            ModelState.Remove("Maquina");
            ModelState.Remove("Grupo");
            ModelState.Remove("Encargado");
            ModelState.Remove("EstadoOperativo");
            ModelState.Remove("IdEmpleadoEncargado");

            idsEncargados = idsEncargados.Distinct().Where(i => i > 0).ToList();
            if (idsEncargados.Count == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos 1 encargado.");
                await CargarSelectLists(asig.IdMaquina);
                return View(asig);
            }
            if (idsEncargados.Count > 5)
            {
                ModelState.AddModelError("", "No puede agregar mas de 5 encargados.");
                await CargarSelectLists(asig.IdMaquina);
                return View(asig);
            }

            asig.EstadoOperativo = "Operativo";
            asig.EsActiva        = true;

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

                foreach (var idEmp in idsEncargados)
                    _context.MaquinaAsignacionEncargados.Add(new MaquinaAsignacionEncargado
                    {
                        IdAsignacion  = asig.IdAsignacion,
                        IdEmpleado    = idEmp,
                        FechaAgregado = DateTime.Now
                    });
                await _context.SaveChangesAsync();

                var grupo   = await _context.Grupos.FindAsync(asig.IdGrupo);
                var maquina = await _context.Maquinas.FindAsync(asig.IdMaquina);
                var emps    = await _context.Empleados.Where(e => idsEncargados.Contains(e.idEmpleado)).ToListAsync();
                var nombres = string.Join(", ", emps.Select(e => $"{e.nombre} {e.paterno}"));

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
        public async Task<IActionResult> Edit(int id, MaquinaAsignacion asig, List<int> idsEncargados)
        {
            if (id != asig.IdAsignacion) return NotFound();
            ModelState.Remove("Maquina");
            ModelState.Remove("Grupo");
            ModelState.Remove("Encargado");
            ModelState.Remove("IdEmpleadoEncargado");

            idsEncargados = idsEncargados.Distinct().Where(i => i > 0).ToList();
            if (idsEncargados.Count == 0)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos 1 encargado.");
                ViewBag.EncargadosActuales = idsEncargados;
                await CargarSelectLists(asig.IdMaquina);
                return View(asig);
            }
            if (idsEncargados.Count > 5)
            {
                ModelState.AddModelError("", "No puede agregar mas de 5 encargados.");
                ViewBag.EncargadosActuales = idsEncargados;
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

                    foreach (var idEmp in idsEncargados)
                        _context.MaquinaAsignacionEncargados.Add(new MaquinaAsignacionEncargado
                        {
                            IdAsignacion  = id,
                            IdEmpleado    = idEmp,
                            FechaAgregado = DateTime.Now
                        });

                    await _context.SaveChangesAsync();

                    var idsAnteriores = actuales.Select(e => e.IdEmpleado).OrderBy(x => x).ToList();
                    if (!idsAnteriores.SequenceEqual(idsEncargados.OrderBy(x => x).ToList()))
                    {
                        var empsNuevos  = await _context.Empleados.Where(e => idsEncargados.Contains(e.idEmpleado)).ToListAsync();
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

            ViewBag.EncargadosActuales = idsEncargados;
            await CargarSelectLists(asig.IdMaquina);
            return View(asig);
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
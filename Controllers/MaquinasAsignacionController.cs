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

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estadoOp, int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.MaquinaAsignaciones
                .Include(a => a.Maquina)
                .Include(a => a.Grupo)
                .Include(a => a.Encargado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(a =>
                    a.Maquina.NumeroMaquina.Contains(buscar) ||
                    a.Maquina.NombreMaquina.Contains(buscar) ||
                    (a.Grupo.area != null && a.Grupo.area.Contains(buscar)) ||
                    (a.Encargado.nombre != null && a.Encargado.nombre.Contains(buscar)) ||
                    (a.Encargado.paterno != null && a.Encargado.paterno.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estadoOp))
                query = query.Where(a => a.EstadoOperativo == estadoOp);

            int total      = await query.CountAsync();
            var asignaciones = await query.OrderByDescending(a => a.FechaAsignacion)
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.EstadoOp     = estadoOp;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
            return View(asignaciones);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var asig = await _context.MaquinaAsignaciones
                .Include(a => a.Maquina)
                .Include(a => a.Grupo)
                .Include(a => a.Encargado)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);

            if (asig == null) return NotFound();
            return View(asig);
        }

        // ── CREATE GET ───────────────────────────────────────────
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

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaquinaAsignacion asig)
        {
            ModelState.Remove("Maquina");
            ModelState.Remove("Grupo");
            ModelState.Remove("Encargado");
            ModelState.Remove("EstadoOperativo");
            asig.EstadoOperativo = "Operativo";
            asig.EsActiva        = true;

            if (ModelState.IsValid)
            {
                // Verificar que la máquina no tenga ya una asignación activa
                var asigExistente = await _context.MaquinaAsignaciones
                    .FirstOrDefaultAsync(a => a.IdMaquina == asig.IdMaquina && a.EsActiva);

                if (asigExistente != null)
                {
                    // Cerrar la asignación anterior
                    var maqAnterior  = await _context.Maquinas.FindAsync(asig.IdMaquina);
                    var grupoAnterior = await _context.Grupos.FindAsync(asigExistente.IdGrupo);
                    var encAnterior   = await _context.Empleados.FindAsync(asigExistente.IdEmpleadoEncargado);

                    asigExistente.EsActiva = false;

                    // Registrar en log el cambio de asignación
                    await RegistrarLog(asig.IdMaquina, "CambioAsignacion",
                        $"Grupo: {grupoAnterior?.area} | Encargado: {encAnterior?.nombre} {encAnterior?.paterno}",
                        "Reasignado a nuevo grupo/encargado",
                        "Asignación anterior cerrada por nueva asignación.");
                }

                _context.MaquinaAsignaciones.Add(asig);
                await _context.SaveChangesAsync();

                var grupo    = await _context.Grupos.FindAsync(asig.IdGrupo);
                var encargado= await _context.Empleados.FindAsync(asig.IdEmpleadoEncargado);
                var maquina  = await _context.Maquinas.FindAsync(asig.IdMaquina);

                await RegistrarLog(asig.IdMaquina, "CambioAsignacion",
                    "Sin asignación",
                    $"Grupo: {grupo?.area} | Encargado: {encargado?.nombre} {encargado?.paterno}",
                    asig.Observaciones ?? "Nueva asignación registrada.");

                await _auditoriaService.RegistrarAsync("Crear", "MaquinaAsignacion", asig.IdAsignacion,
                    $"Asignó máquina {maquina?.NumeroMaquina} al grupo {grupo?.area}");
                await _notifService.NotificarAccionAsync("Creacion", "Asignación Máquina",
                    $"Máquina {maquina?.NumeroMaquina} asignada al grupo {grupo?.area}",
                    $"/Maquinas/Details/{asig.IdMaquina}");

                TempData["Success"] = "Máquina asignada correctamente.";
                return RedirectToAction("Details", "Maquinas", new { id = asig.IdMaquina });
            }

            await CargarSelectLists(asig.IdMaquina);
            return View(asig);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var asig = await _context.MaquinaAsignaciones
                .Include(a => a.Maquina)
                .FirstOrDefaultAsync(a => a.IdAsignacion == id);
            if (asig == null) return NotFound();
            await CargarSelectLists(asig.IdMaquina);
            return View(asig);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaquinaAsignacion asig)
        {
            if (id != asig.IdAsignacion) return NotFound();
            ModelState.Remove("Maquina");
            ModelState.Remove("Grupo");
            ModelState.Remove("Encargado");

            if (ModelState.IsValid)
            {
                // Detectar cambio de encargado
                var original = await _context.MaquinaAsignaciones.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.IdAsignacion == id);

                try
                {
                    _context.Update(asig);
                    await _context.SaveChangesAsync();

                    if (original?.IdEmpleadoEncargado != asig.IdEmpleadoEncargado)
                    {
                        var encAnterior = await _context.Empleados.FindAsync(original?.IdEmpleadoEncargado);
                        var encNuevo    = await _context.Empleados.FindAsync(asig.IdEmpleadoEncargado);
                        await RegistrarLog(asig.IdMaquina, "CambioEncargado",
                            $"{encAnterior?.nombre} {encAnterior?.paterno}",
                            $"{encNuevo?.nombre} {encNuevo?.paterno}",
                            "Cambio de encargado en asignación.");
                    }

                    if (original?.IdGrupo != asig.IdGrupo)
                    {
                        var grupoAnterior = await _context.Grupos.FindAsync(original?.IdGrupo);
                        var grupoNuevo    = await _context.Grupos.FindAsync(asig.IdGrupo);
                        await RegistrarLog(asig.IdMaquina, "CambioAsignacion",
                            grupoAnterior?.area ?? "—",
                            grupoNuevo?.area    ?? "—",
                            "Reasignación a otro grupo.");
                    }

                    await _auditoriaService.RegistrarAsync("Editar", "MaquinaAsignacion", id,
                        $"Editó asignación #{id}");
                    TempData["Success"] = "Asignación actualizada correctamente.";
                    return RedirectToAction("Details", "Maquinas", new { id = asig.IdMaquina });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.MaquinaAsignaciones.AnyAsync(a => a.IdAsignacion == id)) return NotFound();
                    throw;
                }
            }

            await CargarSelectLists(asig.IdMaquina);
            return View(asig);
        }

        // ── HELPER ───────────────────────────────────────────────
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
            ViewBag.EmpleadosList = new SelectList(empleados, "idEmpleado", "Nombre");
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
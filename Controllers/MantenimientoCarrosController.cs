using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class MantenimientoCarrosController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly NotificacionService _notifService;
        private readonly AuditoriaService    _auditoriaService;
        private readonly EmailService        _emailService;

        public MantenimientoCarrosController(
            AppDbContext        context,
            NotificacionService notifService,
            AuditoriaService    auditoriaService,
            EmailService        emailService)
        {
            _context          = context;
            _notifService     = notifService;
            _auditoriaService = auditoriaService;
            _emailService     = emailService;
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

            var mantenimientos = await (orden == "asc"
                ? query.OrderBy(m => m.FechaProgramada)
                : query.OrderByDescending(m => m.FechaProgramada))
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

                // Notificación de creación
                await _notifService.CrearAsync(
                    tipo:    "Creacion",
                    titulo:  $"Nuevo mantenimiento registrado — {vm.Carro?.Placa ?? "vehículo"}",
                    mensaje: $"Se programó un mantenimiento para el {vm.FechaProgramada:dd/MM/yyyy}.",
                    url:     $"/MantenimientoCarros/Details/{vm.IdMante}",
                    idMante: vm.IdMante
                );

                // Auditoría
                await _auditoriaService.RegistrarAsync(
                    accion:      "Crear",
                    entidad:     "MantenimientoCarro",
                    idEntidad:   vm.IdMante,
                    descripcion: $"Registró mantenimiento #{vm.IdMante} para vehículo IdCarro={vm.IdCarro} programado el {vm.FechaProgramada:dd/MM/yyyy}"
                );

                // Si la fecha programada es hoy, enviar email inmediatamente
                if (vm.FechaProgramada.Date == DateTime.Today)
                {
                    var carro = await _context.Carros.FindAsync(vm.IdCarro);
                    var tipo  = await _context.TiposMantenimiento.FindAsync(vm.IdTipoMante);
                    var usuarios = await _context.Usuarios
                        .Where(u => u.activo && u.correo != null).ToListAsync();

                    foreach (var u in usuarios)
                    {
                        await _emailService.EnviarAlertaMantenimientoAsync(
                            destinatario:      u.correo!,
                            nombreUsuario:     u.nombreCompleto ?? u.username,
                            placa:             carro?.Placa ?? "—",
                            tipoMantenimiento: tipo?.Nombre ?? "—",
                            fechaProgramada:   vm.FechaProgramada,
                            idMante:           vm.IdMante
                        );
                    }
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
                idMante: id
            );

            await _auditoriaService.RegistrarAsync(
                accion:      "CambioEstado",
                entidad:     "MantenimientoCarro",
                idEntidad:   id,
                descripcion: $"Cambió mantenimiento #{id} de Pendiente → En proceso"
            );

            TempData["Success"] = "Mantenimiento marcado como En proceso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── CULMINAR (En proceso → Culminado) ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Culminar(int id)
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

            m.Estado         = "Culminado";
            m.FechaCulminada = DateTime.Now;

            // Devolver carro a Activo si no tiene otros mantenimientos en proceso
            bool otrosEnProceso = await _context.MantenimientosCarros
                .AnyAsync(x => x.IdCarro == m.IdCarro && x.Estado == "En proceso" && x.IdMante != id);

            if (!otrosEnProceso)
            {
                var carro = await _context.Carros.FindAsync(m.IdCarro);
                if (carro != null) carro.Estado = "Activo";
            }

            await _context.SaveChangesAsync();

            await _notifService.CrearAsync(
                tipo:    "CambioEstado",
                titulo:  $"Mantenimiento culminado — {m.Carro?.Placa}",
                mensaje: $"El mantenimiento de {m.TipoMantenimiento?.Nombre} fue culminado.",
                url:     $"/MantenimientoCarros/Details/{id}",
                idMante: id
            );

            await _auditoriaService.RegistrarAsync(
                accion:      "CambioEstado",
                entidad:     "MantenimientoCarro",
                idEntidad:   id,
                descripcion: $"Culminó mantenimiento #{id} a las {DateTime.Now:HH:mm}"
            );

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
    }
}
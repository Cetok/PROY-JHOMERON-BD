using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class CarroModalidadesController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly NotificacionService _notifService;

        public CarroModalidadesController(AppDbContext context, NotificacionService notifService)
        {
            _context      = context;
            _notifService = notifService;
        }

        // ── ASIGNAR GET ──────────────────────────────────────────
        public async Task<IActionResult> Asignar(int idCarro)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var asignadas = await _context.CarroModalidades
                .Where(cm => cm.IdCarro == idCarro)
                .Select(cm => cm.IdModalidad)
                .ToListAsync();

            var disponibles = await _context.Modalidades
                .Where(m => !asignadas.Contains(m.IdModalidad) && m.Estado == "Activo")
                .OrderBy(m => m.TipoModalidad)
                .ToListAsync();

            ViewBag.Carro       = carro;
            ViewBag.Disponibles = new SelectList(disponibles, "IdModalidad", "TipoModalidad");

            return View(new CarroModalidad { IdCarro = idCarro, FechaAsignacion = DateTime.Today });
        }

        // ── ASIGNAR POST ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(CarroModalidad vm)
        {
            ModelState.Remove("Carro");
            ModelState.Remove("Modalidad");

            if (ModelState.IsValid)
            {
                bool yaExiste = await _context.CarroModalidades
                    .AnyAsync(cm => cm.IdCarro == vm.IdCarro && cm.IdModalidad == vm.IdModalidad);

                if (yaExiste)
                {
                    TempData["Error"] = "Esta modalidad ya está asignada al vehículo.";
                    return RedirectToAction("Details", "Carros", new { id = vm.IdCarro });
                }

                _context.Add(vm);

                // Registrar en historial
                var modalidad = await _context.Modalidades.FindAsync(vm.IdModalidad);
                var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema";
                _context.CarroModalidadLogs.Add(new CarroModalidadLog
                {
                    IdCarro          = vm.IdCarro,
                    IdModalidad      = vm.IdModalidad,
                    TipoModalidad    = modalidad?.TipoModalidad,
                    Codigo           = modalidad?.Codigo,
                    FechaAsignacion  = vm.FechaAsignacion,
                    FechaVencimiento = vm.FechaVencimiento,
                    FechaRegistro    = DateTime.Now,
                    Accion           = "Asignado",
                    UsuarioNombre    = nombreUsuario
                });

                await _context.SaveChangesAsync();

                // Notificar asignación
                var carro = await _context.Carros.FindAsync(vm.IdCarro);
                await NotificarAdminYSilvana(
                    tipo:    "Creacion",
                    titulo:  $"📋 Modalidad asignada — {carro?.Placa}",
                    mensaje: $"Se asignó la modalidad '{modalidad?.TipoModalidad}' al vehículo {carro?.Placa}. Vencimiento: {vm.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "sin fecha"}.",
                    url:     $"/Carros/Details/{vm.IdCarro}"
                );

                TempData["Success"] = "Modalidad asignada correctamente.";
                return RedirectToAction("Details", "Carros", new { id = vm.IdCarro });
            }

            var carroView = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == vm.IdCarro);
            var asignadas = await _context.CarroModalidades.Where(cm => cm.IdCarro == vm.IdCarro).Select(cm => cm.IdModalidad).ToListAsync();
            var disponibles = await _context.Modalidades.Where(m => !asignadas.Contains(m.IdModalidad) && m.Estado == "Activo").OrderBy(m => m.TipoModalidad).ToListAsync();
            ViewBag.Carro       = carroView;
            ViewBag.Disponibles = new SelectList(disponibles, "IdModalidad", "TipoModalidad", vm.IdModalidad);
            return View(vm);
        }

        // ── EDITAR GET ───────────────────────────────────────────
        public async Task<IActionResult> Editar(int idCarro, int idModalidad)
        {
            var cm = await _context.CarroModalidades
                .Include(x => x.Carro)
                .Include(x => x.Modalidad)
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdModalidad == idModalidad);

            if (cm == null) return NotFound();
            return View(cm);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int idCarro, int idModalidad, CarroModalidad vm)
        {
            ModelState.Remove("Carro");
            ModelState.Remove("Modalidad");

            if (ModelState.IsValid)
            {
                var existing = await _context.CarroModalidades
                    .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdModalidad == idModalidad);

                if (existing == null) return NotFound();

                existing.FechaAsignacion  = vm.FechaAsignacion;
                existing.FechaVencimiento = vm.FechaVencimiento;

                // Actualizar historial
                var modalidad = await _context.Modalidades.FindAsync(idModalidad);
                var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema";
                _context.CarroModalidadLogs.Add(new CarroModalidadLog
                {
                    IdCarro          = idCarro,
                    IdModalidad      = idModalidad,
                    TipoModalidad    = modalidad?.TipoModalidad,
                    Codigo           = modalidad?.Codigo,
                    FechaAsignacion  = vm.FechaAsignacion,
                    FechaVencimiento = vm.FechaVencimiento,
                    FechaRegistro    = DateTime.Now,
                    Accion           = "Actualizado",
                    UsuarioNombre    = nombreUsuario
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Fechas de modalidad actualizadas.";
                return RedirectToAction("Details", "Carros", new { id = idCarro });
            }

            var cm = await _context.CarroModalidades
                .Include(x => x.Carro).Include(x => x.Modalidad)
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdModalidad == idModalidad);
            return View(cm);
        }

        // ── QUITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(int idCarro, int idModalidad)
        {
            var cm = await _context.CarroModalidades
                .Include(x => x.Modalidad)
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdModalidad == idModalidad);

            if (cm != null)
            {
                // Registrar en historial antes de quitar
                var nombreUsuario = HttpContext.Session.GetString("UsuarioNombre") ?? "Sistema";
                _context.CarroModalidadLogs.Add(new CarroModalidadLog
                {
                    IdCarro          = idCarro,
                    IdModalidad      = idModalidad,
                    TipoModalidad    = cm.Modalidad?.TipoModalidad,
                    Codigo           = cm.Modalidad?.Codigo,
                    FechaAsignacion  = cm.FechaAsignacion,
                    FechaVencimiento = cm.FechaVencimiento,
                    FechaRegistro    = DateTime.Now,
                    Accion           = "Removido",
                    UsuarioNombre    = nombreUsuario
                });

                _context.CarroModalidades.Remove(cm);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Modalidad removida del vehículo.";
            }

            return RedirectToAction("Details", "Carros", new { id = idCarro });
        }

        // ── HELPER: Notificar solo Admin y Transporte ────────────
        private async Task NotificarAdminYSilvana(string tipo, string titulo, string mensaje, string url)
        {
            var usuarios = await _context.Usuarios
                .Where(u => u.activo && (u.rol == "Admin" || u.rol == "Transporte"))
                .ToListAsync();

            foreach (var u in usuarios)
            {
                _context.Notificaciones.Add(new Notificacion
                {
                    IdUsuario    = u.idUsuario,
                    Tipo         = tipo,
                    Titulo       = titulo,
                    Mensaje      = mensaje,
                    Url          = url,
                    Leida        = false,
                    FechaCreacion = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}
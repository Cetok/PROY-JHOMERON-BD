using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class EmpleadoSegurosController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly NotificacionService _notifService;

        public EmpleadoSegurosController(AppDbContext context, NotificacionService notifService)
        {
            _context      = context;
            _notifService = notifService;
        }

        // ── ASIGNAR GET ──────────────────────────────────────────
        public async Task<IActionResult> Asignar(int idEmpleado)
        {
            var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.idEmpleado == idEmpleado);
            if (empleado == null) return NotFound();

            var segurosAsignados = await _context.EmpleadoSeguros
                .Where(es => es.IdEmpleado == idEmpleado)
                .Select(es => es.IdSeguro)
                .ToListAsync();

            var segurosDisponibles = await _context.Seguros
                .Where(s => !segurosAsignados.Contains(s.IdSeguro))
                .OrderBy(s => s.TipoSeguro)
                .ToListAsync();

            ViewBag.Empleado           = empleado;
            ViewBag.SegurosDisponibles = new SelectList(segurosDisponibles, "IdSeguro", "TipoSeguro");

            var vm = new EmpleadoSeguro { IdEmpleado = idEmpleado, FechaAsignada = DateTime.Today };
            return View(vm);
        }

        // ── ASIGNAR POST ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(EmpleadoSeguro vm)
        {
            ModelState.Remove("Empleado");
            ModelState.Remove("Seguro");

            if (ModelState.IsValid)
            {
                bool yaExiste = await _context.EmpleadoSeguros
                    .AnyAsync(es => es.IdEmpleado == vm.IdEmpleado && es.IdSeguro == vm.IdSeguro);

                if (yaExiste)
                {
                    TempData["Error"] = "Este seguro ya está asignado al empleado.";
                    return RedirectToAction("Details", "Empleados", new { id = vm.IdEmpleado });
                }

                _context.Add(vm);
                await _context.SaveChangesAsync();
                await _notifService.NotificarAccionAsync("Creacion", "EmpleadoSeguro", "Seguro asignado a empleado");
                TempData["Success"] = "Seguro asignado correctamente al empleado.";
                return RedirectToAction("Details", "Empleados", new { id = vm.IdEmpleado });
            }

            var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.idEmpleado == vm.IdEmpleado);
            var asignados = await _context.EmpleadoSeguros
                .Where(es => es.IdEmpleado == vm.IdEmpleado).Select(es => es.IdSeguro).ToListAsync();
            var disponibles = await _context.Seguros
                .Where(s => !asignados.Contains(s.IdSeguro)).OrderBy(s => s.TipoSeguro).ToListAsync();

            ViewBag.Empleado           = empleado;
            ViewBag.SegurosDisponibles = new SelectList(disponibles, "IdSeguro", "TipoSeguro", vm.IdSeguro);
            return View(vm);
        }

        // ── EDITAR GET ───────────────────────────────────────────
        public async Task<IActionResult> Editar(int idEmpleado, int idSeguro)
        {
            var es = await _context.EmpleadoSeguros
                .Include(x => x.Empleado)
                .Include(x => x.Seguro)
                .FirstOrDefaultAsync(x => x.IdEmpleado == idEmpleado && x.IdSeguro == idSeguro);

            if (es == null) return NotFound();
            return View(es);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int idEmpleado, int idSeguro, EmpleadoSeguro vm)
        {
            ModelState.Remove("Empleado");
            ModelState.Remove("Seguro");

            if (ModelState.IsValid)
            {
                var existing = await _context.EmpleadoSeguros
                    .FirstOrDefaultAsync(x => x.IdEmpleado == idEmpleado && x.IdSeguro == idSeguro);

                if (existing == null) return NotFound();

                existing.FechaAsignada  = vm.FechaAsignada;
                existing.FechaCulminada = vm.FechaCulminada;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Fechas del seguro actualizadas.";
                return RedirectToAction("Details", "Empleados", new { id = idEmpleado });
            }

            var es = await _context.EmpleadoSeguros
                .Include(x => x.Empleado).Include(x => x.Seguro)
                .FirstOrDefaultAsync(x => x.IdEmpleado == idEmpleado && x.IdSeguro == idSeguro);
            return View(es);
        }

        // ── QUITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(int idEmpleado, int idSeguro)
        {
            var es = await _context.EmpleadoSeguros
                .FirstOrDefaultAsync(x => x.IdEmpleado == idEmpleado && x.IdSeguro == idSeguro);

            if (es != null)
            {
                _context.EmpleadoSeguros.Remove(es);
                await _context.SaveChangesAsync();
                await _notifService.NotificarAccionAsync("Eliminacion", "EmpleadoSeguro", "Seguro removido de empleado");
                TempData["Success"] = "Seguro removido del empleado.";
            }

            return RedirectToAction("Details", "Empleados", new { id = idEmpleado });
        }
    }
}
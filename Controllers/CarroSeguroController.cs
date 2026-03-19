using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class CarroSegurosController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly NotificacionService _notifService;

        public CarroSegurosController(AppDbContext context, NotificacionService notifService)
        {
            _context      = context;
            _notifService = notifService;
        }

        // ── ASIGNAR GET ──────────────────────────────────────────
        public async Task<IActionResult> Asignar(int idCarro)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var segurosAsignados = await _context.CarroSeguros
                .Where(cs => cs.IdCarro == idCarro)
                .Select(cs => cs.IdSeguro)
                .ToListAsync();

            var segurosDisponibles = await _context.Seguros
                .Where(s => !segurosAsignados.Contains(s.IdSeguro))
                .OrderBy(s => s.TipoSeguro)
                .ToListAsync();

            ViewBag.Carro              = carro;
            ViewBag.SegurosDisponibles = new SelectList(segurosDisponibles, "IdSeguro", "TipoSeguro");

            var vm = new CarroSeguro { IdCarro = idCarro, FechaAsignada = DateTime.Today };
            return View(vm);
        }

        // ── ASIGNAR POST ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(CarroSeguro vm)
        {
            ModelState.Remove("Carro");
            ModelState.Remove("Seguro");

            if (ModelState.IsValid)
            {
                bool yaExiste = await _context.CarroSeguros
                    .AnyAsync(cs => cs.IdCarro == vm.IdCarro && cs.IdSeguro == vm.IdSeguro);

                if (yaExiste)
                {
                    TempData["Error"] = "Este seguro ya está asignado al vehículo.";
                    return RedirectToAction("Details", "Carros", new { id = vm.IdCarro });
                }

                _context.Add(vm);
                await _context.SaveChangesAsync();
                await _notifService.NotificarAccionAsync("Creacion", "CarroSeguro", "Seguro asignado a vehículo");
                TempData["Success"] = "Seguro asignado correctamente al vehículo.";
                return RedirectToAction("Details", "Carros", new { id = vm.IdCarro });
            }

            // Si falla, recargar vista
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == vm.IdCarro);
            var segurosAsignados = await _context.CarroSeguros
                .Where(cs => cs.IdCarro == vm.IdCarro)
                .Select(cs => cs.IdSeguro).ToListAsync();
            var segurosDisponibles = await _context.Seguros
                .Where(s => !segurosAsignados.Contains(s.IdSeguro))
                .OrderBy(s => s.TipoSeguro).ToListAsync();

            ViewBag.Carro              = carro;
            ViewBag.SegurosDisponibles = new SelectList(segurosDisponibles, "IdSeguro", "TipoSeguro", vm.IdSeguro);
            return View(vm);
        }

        // ── EDITAR GET (fechas) ──────────────────────────────────
        public async Task<IActionResult> Editar(int idCarro, int idSeguro)
        {
            var cs = await _context.CarroSeguros
                .Include(x => x.Carro)
                .Include(x => x.Seguro)
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdSeguro == idSeguro);

            if (cs == null) return NotFound();
            return View(cs);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int idCarro, int idSeguro, CarroSeguro vm)
        {
            ModelState.Remove("Carro");
            ModelState.Remove("Seguro");

            if (ModelState.IsValid)
            {
                var existing = await _context.CarroSeguros
                    .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdSeguro == idSeguro);

                if (existing == null) return NotFound();

                existing.FechaAsignada  = vm.FechaAsignada;
                existing.FechaCulminada = vm.FechaCulminada;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Fechas del seguro actualizadas.";
                return RedirectToAction("Details", "Carros", new { id = idCarro });
            }

            var cs = await _context.CarroSeguros
                .Include(x => x.Carro).Include(x => x.Seguro)
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdSeguro == idSeguro);
            return View(cs);
        }

        // ── QUITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(int idCarro, int idSeguro)
        {
            var cs = await _context.CarroSeguros
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdSeguro == idSeguro);

            if (cs != null)
            {
                _context.CarroSeguros.Remove(cs);
                await _context.SaveChangesAsync();
                await _notifService.NotificarAccionAsync("Eliminacion", "CarroSeguro", "Seguro removido de vehículo");
                TempData["Success"] = "Seguro removido del vehículo.";
            }

            return RedirectToAction("Details", "Carros", new { id = idCarro });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class CarroModalidadesController : Controller
    {
        private readonly AppDbContext _context;

        public CarroModalidadesController(AppDbContext context)
        {
            _context = context;
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

            return View(new CarroModalidad { IdCarro = idCarro });
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
                await _context.SaveChangesAsync();
                TempData["Success"] = "Modalidad asignada al vehículo correctamente.";
                return RedirectToAction("Details", "Carros", new { id = vm.IdCarro });
            }

            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == vm.IdCarro);
            var asignadas = await _context.CarroModalidades.Where(cm => cm.IdCarro == vm.IdCarro).Select(cm => cm.IdModalidad).ToListAsync();
            var disponibles = await _context.Modalidades.Where(m => !asignadas.Contains(m.IdModalidad) && m.Estado == "Activo").OrderBy(m => m.TipoModalidad).ToListAsync();
            ViewBag.Carro       = carro;
            ViewBag.Disponibles = new SelectList(disponibles, "IdModalidad", "TipoModalidad", vm.IdModalidad);
            return View(vm);
        }

        // ── QUITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(int idCarro, int idModalidad)
        {
            var cm = await _context.CarroModalidades
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdModalidad == idModalidad);

            if (cm != null)
            {
                _context.CarroModalidades.Remove(cm);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Modalidad removida del vehículo.";
            }

            return RedirectToAction("Details", "Carros", new { id = idCarro });
        }
    }
}
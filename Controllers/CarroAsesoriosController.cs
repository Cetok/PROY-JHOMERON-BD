using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class CarroAsesoriosController : Controller
    {
        private readonly AppDbContext _context;

        public CarroAsesoriosController(AppDbContext context)
        {
            _context = context;
        }

        // ── ASIGNAR GET ──────────────────────────────────────────
        public async Task<IActionResult> Asignar(int idCarro)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var asignados = await _context.CarroAsesorios
                .Where(ca => ca.IdCarro == idCarro)
                .Select(ca => ca.IdAsesorio)
                .ToListAsync();

            var disponibles = await _context.Asesorios
                .Where(a => !asignados.Contains(a.IdAsesorio))
                .OrderBy(a => a.TipoAsesorio)
                .ToListAsync();

            ViewBag.Carro       = carro;
            ViewBag.Disponibles = new SelectList(disponibles, "IdAsesorio", "TipoAsesorio");

            // Claves en minúscula (id / tipo) para que el JS las encuentre correctamente
            ViewBag.AccesoriosJson = System.Text.Json.JsonSerializer.Serialize(
                disponibles.Select(a => new { id = a.IdAsesorio, tipo = a.TipoAsesorio })
            );

            return View(new CarroAsesorio { IdCarro = idCarro });
        }

        // ── ASIGNAR POST ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(CarroAsesorio vm,
            string? tipoExtintor, string? pesoExtintor, string? fechaVencimientoExtintorStr)
        {
            ModelState.Remove("Carro");
            ModelState.Remove("Asesorio");

            bool existe = await _context.CarroAsesorios
                .AnyAsync(ca => ca.IdCarro == vm.IdCarro && ca.IdAsesorio == vm.IdAsesorio);

            if (existe)
            {
                TempData["Error"] = "Este accesorio ya está asignado al vehículo.";
                return RedirectToAction("Details", "Carros", new { id = vm.IdCarro });
            }

            var accesorio   = await _context.Asesorios.FindAsync(vm.IdAsesorio);
            bool esExtintor = accesorio?.TipoAsesorio?.ToLower().Contains("extintor") == true;

            if (esExtintor)
            {
                vm.TipoExtintor             = tipoExtintor;
                vm.PesoExtintor             = pesoExtintor;
                vm.FechaVencimientoExtintor = DateOnly.TryParse(fechaVencimientoExtintorStr, out var fv) ? fv : null;
            }
            else
            {
                vm.TipoExtintor             = null;
                vm.PesoExtintor             = null;
                vm.FechaVencimientoExtintor = null;
            }

            if (ModelState.IsValid)
            {
                _context.Add(vm);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Accesorio asignado al vehículo correctamente.";
                return RedirectToAction("Details", "Carros", new { id = vm.IdCarro });
            }

            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == vm.IdCarro);
            var asignados = await _context.CarroAsesorios
                .Where(ca => ca.IdCarro == vm.IdCarro).Select(ca => ca.IdAsesorio).ToListAsync();
            var disponibles = await _context.Asesorios
                .Where(a => !asignados.Contains(a.IdAsesorio)).OrderBy(a => a.TipoAsesorio).ToListAsync();

            ViewBag.Carro       = carro;
            ViewBag.Disponibles = new SelectList(disponibles, "IdAsesorio", "TipoAsesorio", vm.IdAsesorio);
            ViewBag.AccesoriosJson = System.Text.Json.JsonSerializer.Serialize(
                disponibles.Select(a => new { id = a.IdAsesorio, tipo = a.TipoAsesorio })
            );
            return View(vm);
        }

        // ── EDITAR GET ───────────────────────────────────────────
        public async Task<IActionResult> Editar(int idCarro, int idAsesorio)
        {
            var ca = await _context.CarroAsesorios
                .Include(x => x.Carro)
                .Include(x => x.Asesorio)
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdAsesorio == idAsesorio);

            if (ca == null) return NotFound();

            ViewBag.EsExtintor = ca.Asesorio?.TipoAsesorio?.ToLower().Contains("extintor") == true;
            return View(ca);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int idCarro, int idAsesorio, CarroAsesorio vm,
            string? tipoExtintor, string? pesoExtintor, string? fechaVencimientoExtintorStr)
        {
            ModelState.Remove("Carro");
            ModelState.Remove("Asesorio");

            if (ModelState.IsValid)
            {
                var existing = await _context.CarroAsesorios
                    .Include(x => x.Asesorio)
                    .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdAsesorio == idAsesorio);

                if (existing == null) return NotFound();

                existing.FechaAsignada = vm.FechaAsignada;
                existing.Observaciones = vm.Observaciones;

                bool esExtintor = existing.Asesorio?.TipoAsesorio?.ToLower().Contains("extintor") == true;
                if (esExtintor)
                {
                    existing.TipoExtintor             = tipoExtintor;
                    existing.PesoExtintor             = pesoExtintor;
                    existing.FechaVencimientoExtintor = DateOnly.TryParse(fechaVencimientoExtintorStr, out var fv) ? fv : null;
                }
                else
                {
                    existing.TipoExtintor             = null;
                    existing.PesoExtintor             = null;
                    existing.FechaVencimientoExtintor = null;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Accesorio actualizado.";
                return RedirectToAction("Details", "Carros", new { id = idCarro });
            }

            var ca = await _context.CarroAsesorios
                .Include(x => x.Carro).Include(x => x.Asesorio)
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdAsesorio == idAsesorio);

            ViewBag.EsExtintor = ca?.Asesorio?.TipoAsesorio?.ToLower().Contains("extintor") == true;
            return View(vm);
        }

        // ── QUITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(int idCarro, int idAsesorio)
        {
            var ca = await _context.CarroAsesorios
                .FirstOrDefaultAsync(x => x.IdCarro == idCarro && x.IdAsesorio == idAsesorio);

            if (ca != null)
            {
                _context.CarroAsesorios.Remove(ca);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Accesorio quitado del vehículo.";
            }
            return RedirectToAction("Details", "Carros", new { id = idCarro });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class CarrosController : Controller
    {
        private readonly AppDbContext _context;

        public CarrosController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? buscar, string? estado, string? categoria, string? orden = "az", int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.Carros.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c =>
                    c.Placa.Contains(buscar) || c.Marca.Contains(buscar) || c.Modelo.Contains(buscar) ||
                    (c.NumeroMotor != null && c.NumeroMotor.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(c => c.Estado == estado);

            if (!string.IsNullOrWhiteSpace(categoria))
                query = query.Where(c => c.Categoria == categoria);

            int total = await query.CountAsync();
            var carros = await (orden == "za"
                ? query.OrderByDescending(c => c.Placa)
                : query.OrderBy(c => c.Placa))
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

            var categorias = await _context.Carros
                .Where(c => c.Categoria != null).Select(c => c.Categoria!)
                .Distinct().OrderBy(c => c).ToListAsync();

            ViewBag.Buscar = buscar; ViewBag.Estado = estado; ViewBag.Categoria = categoria;
            ViewBag.Categorias = categorias; ViewBag.Pagina = pagina; ViewBag.Total = total;
            ViewBag.Orden = orden;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
            return View(carros);
        }

        public async Task<IActionResult> Details(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            return View(carro);
        }

        public IActionResult Create() => View();

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Carro carro)
        {
            ModelState.Remove("EmpleadosCarros"); ModelState.Remove("CarroSeguros");
            ModelState.Remove("CarroAsesorios"); ModelState.Remove("CarroModalidades");
            ModelState.Remove("MantenimientosCarros");

            if (ModelState.IsValid)
            {
                if (await _context.Carros.AnyAsync(c => c.Placa == carro.Placa))
                { ModelState.AddModelError("Placa", "Ya existe un vehículo con esa placa."); return View(carro); }

                _context.Add(carro);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Vehículo {carro.Placa} registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = carro.IdCarro });
            }
            return View(carro);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            return View(carro);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Carro carro)
        {
            if (id != carro.IdCarro) return NotFound();
            ModelState.Remove("EmpleadosCarros"); ModelState.Remove("CarroSeguros");
            ModelState.Remove("CarroAsesorios"); ModelState.Remove("CarroModalidades");
            ModelState.Remove("MantenimientosCarros");

            if (ModelState.IsValid)
            {
                if (await _context.Carros.AnyAsync(c => c.Placa == carro.Placa && c.IdCarro != id))
                { ModelState.AddModelError("Placa", "Ya existe otro vehículo con esa placa."); return View(carro); }

                try { _context.Update(carro); await _context.SaveChangesAsync();
                    TempData["Success"] = $"Vehículo {carro.Placa} actualizado.";
                    return RedirectToAction(nameof(Details), new { id = carro.IdCarro }); }
                catch (DbUpdateConcurrencyException)
                { if (!await _context.Carros.AnyAsync(c => c.IdCarro == id)) return NotFound(); throw; }
            }
            return View(carro);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            return View(carro);
        }

        [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            try { _context.Carros.Remove(carro); await _context.SaveChangesAsync();
                TempData["Success"] = $"Vehículo {carro.Placa} eliminado."; }
            catch (DbUpdateException)
            { TempData["Error"] = "No se puede eliminar: tiene registros asociados.";
                return RedirectToAction(nameof(Details), new { id }); }
            return RedirectToAction(nameof(Index));
        }
    }
}
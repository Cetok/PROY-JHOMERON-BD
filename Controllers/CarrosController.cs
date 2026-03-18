using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estado, string? categoria, int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.Carros
                .Include(c => c.EmpleadosCarros).ThenInclude(ec => ec.Empleado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c =>
                    c.Placa.Contains(buscar) || c.Marca.Contains(buscar) || c.Modelo.Contains(buscar) ||
                    (c.NumeroMotor != null && c.NumeroMotor.Contains(buscar)) ||
                    c.EmpleadosCarros.Any(ec =>
                        (ec.Empleado.nombre != null && ec.Empleado.nombre.Contains(buscar)) ||
                        (ec.Empleado.paterno != null && ec.Empleado.paterno.Contains(buscar))));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(c => c.Estado == estado);

            if (!string.IsNullOrWhiteSpace(categoria))
                query = query.Where(c => c.Categoria == categoria);

            int total = await query.CountAsync();
            var carros = await query
                .OrderBy(c => c.Placa)
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

            var categorias = await _context.Carros
                .Where(c => c.Categoria != null).Select(c => c.Categoria!)
                .Distinct().OrderBy(c => c).ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Estado       = estado;
            ViewBag.Categoria    = categoria;
            ViewBag.Categorias   = categorias;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
            return View(carros);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var carro = await _context.Carros
                .Include(c => c.CarroSeguros).ThenInclude(cs => cs.Seguro)
                .Include(c => c.MantenimientosCarros).ThenInclude(m => m.TipoMantenimiento)
                .Include(c => c.CarroAsesorios).ThenInclude(ca => ca.Asesorio)
                .Include(c => c.CarroModalidades).ThenInclude(cm => cm.Modalidad)
                .Include(c => c.EmpleadosCarros).ThenInclude(ec => ec.Empleado)
                .FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            return View(carro);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            await CargarEmpleados();
            return View();
        }

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Carro carro, int? IdEmpleado)
        {
            ModelState.Remove("EmpleadosCarros");
            ModelState.Remove("CarroSeguros");
            ModelState.Remove("CarroAsesorios");
            ModelState.Remove("CarroModalidades");
            ModelState.Remove("MantenimientosCarros");
            ModelState.Remove("Estado");

            carro.Estado = "Activo";

            if (ModelState.IsValid)
            {
                if (await _context.Carros.AnyAsync(c => c.Placa == carro.Placa))
                {
                    ModelState.AddModelError("Placa", "Ya existe un vehículo con esa placa.");
                    await CargarEmpleados(IdEmpleado);
                    return View(carro);
                }

                _context.Add(carro);
                await _context.SaveChangesAsync();

                // Asignar empleado si se seleccionó uno
                if (IdEmpleado.HasValue && IdEmpleado.Value > 0)
                {
                    _context.EmpleadosCarros.Add(new EmpleadoCarro
                    {
                        IdCarro    = carro.IdCarro,
                        IdEmpleado = IdEmpleado.Value
                    });
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Vehículo {carro.Placa} registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = carro.IdCarro });
            }
            await CargarEmpleados(IdEmpleado);
            return View(carro);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var carro = await _context.Carros
                .Include(c => c.EmpleadosCarros)
                .FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();

            var empleadoActual = carro.EmpleadosCarros.FirstOrDefault()?.IdEmpleado;
            await CargarEmpleados(empleadoActual);
            return View(carro);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Carro carro, int? IdEmpleado)
        {
            if (id != carro.IdCarro) return NotFound();
            ModelState.Remove("EmpleadosCarros");
            ModelState.Remove("CarroSeguros");
            ModelState.Remove("CarroAsesorios");
            ModelState.Remove("CarroModalidades");
            ModelState.Remove("MantenimientosCarros");

            if (ModelState.IsValid)
            {
                if (await _context.Carros.AnyAsync(c => c.Placa == carro.Placa && c.IdCarro != id))
                {
                    ModelState.AddModelError("Placa", "Ya existe otro vehículo con esa placa.");
                    await CargarEmpleados(IdEmpleado);
                    return View(carro);
                }

                try
                {
                    _context.Update(carro);

                    // Reemplazar relación empleado-carro
                    var relacionesActuales = await _context.EmpleadosCarros
                        .Where(ec => ec.IdCarro == id).ToListAsync();
                    _context.EmpleadosCarros.RemoveRange(relacionesActuales);

                    if (IdEmpleado.HasValue && IdEmpleado.Value > 0)
                    {
                        _context.EmpleadosCarros.Add(new EmpleadoCarro
                        {
                            IdCarro    = id,
                            IdEmpleado = IdEmpleado.Value
                        });
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Vehículo {carro.Placa} actualizado.";
                    return RedirectToAction(nameof(Details), new { id = carro.IdCarro });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Carros.AnyAsync(c => c.IdCarro == id)) return NotFound();
                    throw;
                }
            }
            await CargarEmpleados(IdEmpleado);
            return View(carro);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            return View(carro);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            try
            {
                _context.Carros.Remove(carro);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Vehículo {carro.Placa} eliminado.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: tiene registros asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        // ── HELPER ───────────────────────────────────────────────
        private async Task CargarEmpleados(int? seleccionado = null)
        {
            var empleados = await _context.Empleados
                .Where(e => e.estado == "Activo")
                .OrderBy(e => e.paterno)
                .Select(e => new {
                    e.idEmpleado,
                    NombreCompleto = e.nombre + " " + e.paterno + " " + e.materno
                })
                .ToListAsync();

            ViewBag.EmpleadosList = new SelectList(empleados, "idEmpleado", "NombreCompleto", seleccionado);
        }
    }
}
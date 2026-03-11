using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace TuProyecto.Controllers
{
    public class EmpleadosController : Controller
    {
        private readonly AppDbContext _context;

        public EmpleadosController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estado, int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Empleados.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(e =>
                    (e.nombre != null && e.nombre.Contains(buscar)) ||
                    (e.paterno != null && e.paterno.Contains(buscar)) ||
                    (e.dni != null && e.dni.Contains(buscar)) ||
                    (e.correo != null && e.correo.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(e => e.estado == estado);

            int total = await query.CountAsync();

            var empleados = await query
                .OrderBy(e => e.paterno)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar   = buscar;
            ViewBag.Estado   = estado;
            ViewBag.Pagina   = pagina;
            ViewBag.Total    = total;
            ViewBag.PorPagina = porPagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(empleados);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.idEmpleado == id);

            if (empleado == null) return NotFound();

            return View(empleado);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create()
        {
            return View();
        }

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Empleado empleado)
        {
            if (ModelState.IsValid)
            {
                // Verificar DNI duplicado
                bool dniExiste = await _context.Empleados
                    .AnyAsync(e => e.dni == empleado.dni);

                if (dniExiste)
                {
                    ModelState.AddModelError("dni", "Ya existe un empleado con ese DNI.");
                    return View(empleado);
                }

                _context.Add(empleado);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Empleado {empleado.nombre} {empleado.paterno} registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(empleado);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.idEmpleado == id);

            if (empleado == null) return NotFound();

            return View(empleado);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Empleado empleado)
        {
            if (id != empleado.idEmpleado) return NotFound();

            if (ModelState.IsValid)
            {
                // Verificar DNI duplicado (excluyendo el mismo)
                bool dniExiste = await _context.Empleados
                    .AnyAsync(e => e.dni == empleado.dni && e.idEmpleado != id);

                if (dniExiste)
                {
                    ModelState.AddModelError("dni", "Ya existe otro empleado con ese DNI.");
                    return View(empleado);
                }

                try
                {
                    _context.Update(empleado);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Empleado {empleado.nombre} {empleado.paterno} actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Empleados.AnyAsync(e => e.idEmpleado == id))
                        return NotFound();
                    throw;
                }
            }

            return View(empleado);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.idEmpleado == id);

            if (empleado == null) return NotFound();

            return View(empleado);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empleado = await _context.Empleados
                .FirstOrDefaultAsync(e => e.idEmpleado == id);

            if (empleado == null) return NotFound();

            try
            {
                _context.Empleados.Remove(empleado);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Empleado {empleado.nombre} {empleado.paterno} eliminado.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este empleado porque tiene registros asociados.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
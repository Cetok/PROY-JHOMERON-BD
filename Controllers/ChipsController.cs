using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class ChipsController : Controller
    {
        private readonly AppDbContext _context;

        public ChipsController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? orden = "az", int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.Chips.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(c => c.NumeroCelular.Contains(buscar));

            int total = await query.CountAsync();

            var chips = await (orden == "za"
                ? query.OrderByDescending(c => c.NumeroCelular)
                : query.OrderBy(c => c.NumeroCelular))
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            // Para cada chip saber si está asignado actualmente
            var idsAsignados = await _context.Asignaciones
                .Where(a => a.IdChip != null && a.EstadoAsignacion == "Activo")
                .Select(a => a.IdChip!.Value)
                .ToListAsync();

            ViewBag.IdsAsignados = idsAsignados;
            ViewBag.Buscar       = buscar;
            ViewBag.Orden        = orden;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(chips);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var chip = await _context.Chips
                .FirstOrDefaultAsync(c => c.IdChip == id);

            if (chip == null) return NotFound();

            // Historial de asignaciones de este chip
            var asignaciones = await _context.Asignaciones
                .Include(a => a.Empleado)
                .Include(a => a.Equipo)
                .Where(a => a.IdChip == id)
                .OrderByDescending(a => a.FechaAsignacion)
                .ToListAsync();

            ViewBag.Asignaciones = asignaciones;

            return View(chip);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chip chip)
        {
            if (ModelState.IsValid)
            {
                bool existe = await _context.Chips
                    .AnyAsync(c => c.NumeroCelular == chip.NumeroCelular);

                if (existe)
                {
                    ModelState.AddModelError("NumeroCelular", "Ya existe un chip con ese número celular.");
                    return View(chip);
                }

                _context.Add(chip);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Chip {chip.NumeroCelular} registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(chip);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var chip = await _context.Chips
                .FirstOrDefaultAsync(c => c.IdChip == id);

            if (chip == null) return NotFound();
            return View(chip);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Chip chip)
        {
            if (id != chip.IdChip) return NotFound();

            if (ModelState.IsValid)
            {
                bool existe = await _context.Chips
                    .AnyAsync(c => c.NumeroCelular == chip.NumeroCelular && c.IdChip != id);

                if (existe)
                {
                    ModelState.AddModelError("NumeroCelular", "Ya existe otro chip con ese número.");
                    return View(chip);
                }

                try
                {
                    _context.Update(chip);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Chip {chip.NumeroCelular} actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Chips.AnyAsync(c => c.IdChip == id))
                        return NotFound();
                    throw;
                }
            }
            return View(chip);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var chip = await _context.Chips
                .FirstOrDefaultAsync(c => c.IdChip == id);

            if (chip == null) return NotFound();

            ViewBag.TotalAsignaciones = await _context.Asignaciones
                .CountAsync(a => a.IdChip == id);

            return View(chip);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chip = await _context.Chips
                .FirstOrDefaultAsync(c => c.IdChip == id);

            if (chip == null) return NotFound();

            try
            {
                _context.Chips.Remove(chip);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Chip {chip.NumeroCelular} eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este chip porque tiene asignaciones asociadas.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
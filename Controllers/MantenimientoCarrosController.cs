using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class MantenimientoCarrosController : Controller
    {
        private readonly AppDbContext _context;

        public MantenimientoCarrosController(AppDbContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estadoFiltro, string? orden = "desc", int pagina = 1)
        {
            int porPagina = 10;

            var query = _context.MantenimientosCarros
                .Include(m => m.Carro)
                .Include(m => m.TipoMantenimiento)
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
                ? query.OrderBy(m => m.FechaMante)
                : query.OrderByDescending(m => m.FechaMante))
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
                FechaMante = DateTime.Today,
                Estado     = "En proceso",
                IdCarro    = idCarro ?? 0
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

            if (ModelState.IsValid)
            {
                vm.Estado = "En proceso";

                // Actualizar estado del carro a "En mantenimiento"
                var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == vm.IdCarro);
                if (carro != null)
                {
                    carro.Estado = "En mantenimiento";
                }

                _context.Add(vm);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mantenimiento registrado. El vehículo está en mantenimiento.";
                return RedirectToAction(nameof(Details), new { id = vm.IdMante });
            }

            await CargarListas(vm.IdCarro);
            return View(vm);
        }

        // ── CULMINAR POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Culminar(int id)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro)
                .FirstOrDefaultAsync(x => x.IdMante == id);

            if (m == null) return NotFound();

            m.Estado         = "Culminado";
            m.FechaCulminada = DateTime.Today;

            // Volver el carro a "Activo" si no tiene otros mantenimientos en proceso
            bool otrosEnProceso = await _context.MantenimientosCarros
                .AnyAsync(x => x.IdCarro == m.IdCarro && x.Estado == "En proceso" && x.IdMante != id);

            if (!otrosEnProceso && m.Carro != null)
                m.Carro.Estado = "Activo";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Mantenimiento culminado. El vehículo volvió a estado Activo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── CANCELAR POST ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro)
                .FirstOrDefaultAsync(x => x.IdMante == id);

            if (m == null) return NotFound();

            m.Estado = "Cancelado";

            bool otrosEnProceso = await _context.MantenimientosCarros
                .AnyAsync(x => x.IdCarro == m.IdCarro && x.Estado == "En proceso" && x.IdMante != id);

            if (!otrosEnProceso && m.Carro != null)
                m.Carro.Estado = "Activo";

            await _context.SaveChangesAsync();
            TempData["Warning"] = "Mantenimiento cancelado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _context.MantenimientosCarros.FirstOrDefaultAsync(x => x.IdMante == id);
            if (m == null) return NotFound();

            if (m.Estado != "En proceso")
            {
                TempData["Warning"] = "Solo se pueden editar mantenimientos en proceso.";
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

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.MantenimientosCarros.FirstOrDefaultAsync(x => x.IdMante == id);
                    if (existing == null) return NotFound();

                    existing.IdTipoMante   = vm.IdTipoMante;
                    existing.FechaMante    = vm.FechaMante;
                    existing.Observaciones = vm.Observaciones;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Mantenimiento actualizado.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.MantenimientosCarros.AnyAsync(x => x.IdMante == id)) return NotFound();
                    throw;
                }
            }

            await CargarListas(vm.IdCarro, vm.IdTipoMante);
            return View(vm);
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.MantenimientosCarros
                .Include(x => x.Carro)
                .Include(x => x.TipoMantenimiento)
                .FirstOrDefaultAsync(x => x.IdMante == id);

            if (m == null) return NotFound();
            return View(m);
        }

        // ── DELETE POST ──────────────────────────────────────────
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

            _context.MantenimientosCarros.Remove(m);
            await _context.SaveChangesAsync();

            // Si era el único en proceso, devolver el carro a Activo
            if (eraEnProceso)
            {
                bool otrosEnProceso = await _context.MantenimientosCarros
                    .AnyAsync(x => x.IdCarro == idCarro && x.Estado == "En proceso");

                if (!otrosEnProceso)
                {
                    var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
                    if (carro != null)
                    {
                        carro.Estado = "Activo";
                        await _context.SaveChangesAsync();
                    }
                }
            }

            TempData["Success"] = "Mantenimiento eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPER ───────────────────────────────────────────────
        private async Task CargarListas(int? idCarroSel = null, int? idTipoSel = null)
        {
            var carros = await _context.Carros
                .OrderBy(c => c.Placa)
                .Select(c => new { c.IdCarro, Descripcion = c.Placa + " — " + c.Marca + " " + c.Modelo })
                .ToListAsync();

            var tipos = await _context.TiposMantenimiento
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            ViewBag.CarrosList = new SelectList(carros, "IdCarro", "Descripcion", idCarroSel);
            ViewBag.TiposList  = new SelectList(tipos,  "IdTipoMante", "Nombre", idTipoSel);
        }
    }
}
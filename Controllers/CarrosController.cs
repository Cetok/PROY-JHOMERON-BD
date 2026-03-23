using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class CarrosController : Controller
    {
        private readonly AppDbContext     _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public CarrosController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
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
                .OrderByDescending(c => c.IdCarro)
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

            // Historial de estados
            var estadoLog = await _context.CarroEstadoLogs
                .Where(l => l.IdCarro == id)
                .OrderByDescending(l => l.FechaHora)
                .ToListAsync();

            ViewBag.EstadoLog = estadoLog;

            // Lista de empleados activos para el select de conductor
            var empleados = await _context.Empleados
                .Where(e => e.estado == "Activo")
                .OrderBy(e => e.paterno)
                .Select(e => new {
                    e.idEmpleado,
                    NombreCompleto = e.nombre + " " + e.paterno + " " + e.materno
                })
                .ToListAsync();
            ViewBag.EmpleadosList = new SelectList(empleados, "idEmpleado", "NombreCompleto");

            return View(carro);
        }

        // ── ASIGNAR CONDUCTOR POST ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarConductor(int idCarro, int? idEmpleado)
        {
            // Remover conductor actual
            var actuales = await _context.EmpleadosCarros
                .Where(ec => ec.IdCarro == idCarro).ToListAsync();
            _context.EmpleadosCarros.RemoveRange(actuales);

            if (idEmpleado.HasValue && idEmpleado.Value > 0)
            {
                _context.EmpleadosCarros.Add(new EmpleadoCarro
                {
                    IdCarro    = idCarro,
                    IdEmpleado = idEmpleado.Value
                });
                await _auditoriaService.RegistrarAsync("Editar", "Carro", idCarro,
                    $"Asignó conductor IdEmpleado={idEmpleado} al vehículo #{idCarro}");
                await _notifService.NotificarAccionAsync("Creacion", "Conductor", "Conductor asignado a vehículo");
            TempData["Success"] = "Conductor asignado correctamente.";
            }
            else
            {
                await _auditoriaService.RegistrarAsync("Editar", "Carro", idCarro,
                    $"Removió conductor del vehículo #{idCarro}");
                await _notifService.NotificarAccionAsync("Eliminacion", "Conductor", "Conductor removido del vehículo");
            TempData["Success"] = "Conductor removido del vehículo.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = idCarro });
        }

        // ── CAMBIAR ESTADO POST ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int idCarro, string nuevoEstado, string motivo, string? observaciones)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            // Solo permite Activo ↔ Inactivo desde este panel
            var estadosPermitidos = new[] { "Activo", "Inactivo" };
            if (!estadosPermitidos.Contains(nuevoEstado))
            {
                TempData["Error"] = "Estado no permitido desde este panel.";
                return RedirectToAction(nameof(Details), new { id = idCarro });
            }

            var estadoAnterior = carro.Estado;
            carro.Estado = nuevoEstado;

            var idStr    = HttpContext.Session.GetString("UsuarioId");
            var nombre   = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            _context.CarroEstadoLogs.Add(new CarroEstadoLog
            {
                IdCarro        = idCarro,
                IdUsuario      = idUsuario,
                NombreUsuario  = nombre,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo    = nuevoEstado,
                Motivo         = motivo,
                Observaciones  = observaciones,
                FechaHora      = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("CambioEstado", "Carro", idCarro,
                $"Cambió estado vehículo #{idCarro} de {estadoAnterior} → {nuevoEstado}. Motivo: {motivo}");

            TempData["Success"] = $"Estado cambiado a \"{nuevoEstado}\". Registrado en historial.";
            return RedirectToAction(nameof(Details), new { id = idCarro });
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Carro carro)
        {
            ModelState.Remove("EmpleadosCarros");
            ModelState.Remove("CarroSeguros");
            ModelState.Remove("CarroAsesorios");
            ModelState.Remove("CarroModalidades");
            ModelState.Remove("MantenimientosCarros");
            ModelState.Remove("Estado");

            // Siempre Activo al registrar
            carro.Estado = "Activo";

            if (ModelState.IsValid)
            {
                if (await _context.Carros.AnyAsync(c => c.Placa == carro.Placa))
                {
                    ModelState.AddModelError("Placa", "Ya existe un vehículo con esa placa.");
                    return View(carro);
                }

                _context.Add(carro);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Crear", "Carro", carro.IdCarro,
                    $"Registró vehículo {carro.Placa} — {carro.Marca} {carro.Modelo}");

                await _notifService.NotificarAccionAsync("Creacion", "Carro", $"Registró vehículo {carro.Placa} — {carro.Marca} {carro.Modelo}", $"/Carros/Details/{carro.IdCarro}");
                TempData["Success"] = $"Vehículo {carro.Placa} registrado correctamente.";
                return RedirectToAction(nameof(Details), new { id = carro.IdCarro });
            }
            return View(carro);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == id);
            if (carro == null) return NotFound();
            return View(carro);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Carro carro)
        {
            if (id != carro.IdCarro) return NotFound();
            ModelState.Remove("EmpleadosCarros");
            ModelState.Remove("CarroSeguros");
            ModelState.Remove("CarroAsesorios");
            ModelState.Remove("CarroModalidades");
            ModelState.Remove("MantenimientosCarros");
            ModelState.Remove("Estado");

            if (ModelState.IsValid)
            {
                if (await _context.Carros.AnyAsync(c => c.Placa == carro.Placa && c.IdCarro != id))
                {
                    ModelState.AddModelError("Placa", "Ya existe otro vehículo con esa placa.");
                    return View(carro);
                }

                try
                {
                    // Preservar estado actual — no se edita desde aquí
                    var estadoActual = await _context.Carros
                        .Where(c => c.IdCarro == id).Select(c => c.Estado).FirstAsync();
                    carro.Estado = estadoActual;

                    _context.Update(carro);
                    await _context.SaveChangesAsync();

                    await _auditoriaService.RegistrarAsync("Editar", "Carro", id,
                        $"Editó vehículo {carro.Placa}");

                    await _notifService.NotificarAccionAsync("Edicion", "Carro", $"Editó vehículo {carro.Placa}", $"/Carros/Details/{id}");
                    TempData["Success"] = $"Vehículo {carro.Placa} actualizado.";
                    return RedirectToAction(nameof(Details), new { id = carro.IdCarro });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Carros.AnyAsync(c => c.IdCarro == id)) return NotFound();
                    throw;
                }
            }
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
                await _auditoriaService.RegistrarAsync("Eliminar", "Carro", id,
                    $"Eliminó vehículo {carro.Placa}");
                await _notifService.NotificarAccionAsync("Eliminacion", "Carro", $"Eliminó vehículo {carro.Placa}");
                TempData["Success"] = $"Vehículo {carro.Placa} eliminado.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: tiene registros asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
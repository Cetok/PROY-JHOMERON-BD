using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class MaquinasController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public MaquinasController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? buscar, string? estado, int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.Maquinas
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Grupo : null)
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Encargado : null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(m =>
                    m.NumeroMaquina.Contains(buscar) ||
                    m.NombreMaquina.Contains(buscar) ||
                    (m.Marca != null && m.Marca.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(m => m.Estado == estado);

            int total    = await query.CountAsync();
            var maquinas = await query.OrderByDescending(m => m.IdMaquina)
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.Estado       = estado;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);
            return View(maquinas);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var maquina = await _context.Maquinas
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Grupo : null)
                .Include(m => m.AsignacionActual).ThenInclude(a => a != null ? a.Encargado : null)
                .Include(m => m.Logs.OrderByDescending(l => l.FechaHora))
                .FirstOrDefaultAsync(m => m.IdMaquina == id);

            if (maquina == null) return NotFound();
            return View(maquina);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public IActionResult Create() => View();

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Maquina maquina)
        {
            ModelState.Remove("AsignacionActual");
            ModelState.Remove("Logs");
            ModelState.Remove("Estado");
            maquina.Estado        = "Activo";
            maquina.FechaRegistro = DateTime.Now;

            var idStr = HttpContext.Session.GetString("UsuarioId");
            maquina.IdUsuarioCreador = int.TryParse(idStr, out int uid) ? uid : null;

            if (ModelState.IsValid)
            {
                // Validar número de máquina único
                if (await _context.Maquinas.AnyAsync(m => m.NumeroMaquina == maquina.NumeroMaquina))
                {
                    ModelState.AddModelError("NumeroMaquina", "Ya existe una máquina con ese número.");
                    return View(maquina);
                }

                _context.Add(maquina);
                await _context.SaveChangesAsync();

                // Log de creación
                await RegistrarLog(maquina.IdMaquina, "Edicion", null, "Registro inicial", "Máquina registrada en el sistema.");

                await _auditoriaService.RegistrarAsync("Crear", "Maquina", maquina.IdMaquina,
                    $"Registró máquina {maquina.NumeroMaquina} — {maquina.NombreMaquina}");
                await _notifService.NotificarAccionAsync("Creacion", "Máquina",
                    $"Se registró la máquina {maquina.NumeroMaquina} — {maquina.NombreMaquina}",
                    $"/Maquinas/Details/{maquina.IdMaquina}");

                TempData["Success"] = $"Máquina {maquina.NumeroMaquina} registrada correctamente.";
                return RedirectToAction(nameof(Details), new { id = maquina.IdMaquina });
            }
            return View(maquina);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var maquina = await _context.Maquinas.FirstOrDefaultAsync(m => m.IdMaquina == id);
            if (maquina == null) return NotFound();
            return View(maquina);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Maquina maquina)
        {
            if (id != maquina.IdMaquina) return NotFound();
            ModelState.Remove("AsignacionActual");
            ModelState.Remove("Logs");

            if (ModelState.IsValid)
            {
                if (await _context.Maquinas.AnyAsync(m => m.NumeroMaquina == maquina.NumeroMaquina && m.IdMaquina != id))
                {
                    ModelState.AddModelError("NumeroMaquina", "Ya existe otra máquina con ese número.");
                    return View(maquina);
                }

                try
                {
                    // Preservar estado actual
                    var estadoActual = await _context.Maquinas
                        .Where(m => m.IdMaquina == id).Select(m => m.Estado).FirstAsync();
                    maquina.Estado = estadoActual;

                    _context.Update(maquina);
                    await _context.SaveChangesAsync();

                    await RegistrarLog(id, "Edicion", "Datos actualizados", maquina.NombreMaquina, "Se editaron los datos básicos.");
                    await _auditoriaService.RegistrarAsync("Editar", "Maquina", id,
                        $"Editó máquina {maquina.NumeroMaquina}");
                    await _notifService.NotificarAccionAsync("Edicion", "Máquina",
                        $"Se actualizó la máquina {maquina.NumeroMaquina}", $"/Maquinas/Details/{id}");

                    TempData["Success"] = "Máquina actualizada correctamente.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Maquinas.AnyAsync(m => m.IdMaquina == id)) return NotFound();
                    throw;
                }
            }
            return View(maquina);
        }

        // ── CAMBIAR ESTADO POST (modal) ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int idMaquina, string nuevoEstado, string? observaciones)
        {
            var maquina = await _context.Maquinas
                .Include(m => m.AsignacionActual)
                .FirstOrDefaultAsync(m => m.IdMaquina == idMaquina);
            if (maquina == null) return NotFound();

            var estadoAnterior = maquina.Estado;
            maquina.Estado = nuevoEstado;

            // Si pasa a inoperativo, marcar asignación como inactiva
            if (nuevoEstado == "Inoperativo" && maquina.AsignacionActual != null)
                maquina.AsignacionActual.EstadoOperativo = "Inactivo";

            // Si vuelve a activo, reactivar asignación
            if (nuevoEstado == "Activo" && maquina.AsignacionActual != null)
                maquina.AsignacionActual.EstadoOperativo = "Operativo";

            await _context.SaveChangesAsync();

            await RegistrarLog(idMaquina, "CambioEstado",
                estadoAnterior, nuevoEstado, observaciones ?? $"Estado cambiado a {nuevoEstado}");

            await _auditoriaService.RegistrarAsync("CambioEstado", "Maquina", idMaquina,
                $"Máquina #{idMaquina}: {estadoAnterior} → {nuevoEstado}");
            await _notifService.NotificarAccionAsync("CambioEstado", "Máquina",
                $"Máquina {maquina.NumeroMaquina} cambió a estado: {nuevoEstado}",
                $"/Maquinas/Details/{idMaquina}");

            TempData["Success"] = $"Estado cambiado a '{nuevoEstado}'. Registrado en historial.";
            return RedirectToAction(nameof(Details), new { id = idMaquina });
        }

        // ── DELETE GET ───────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var maquina = await _context.Maquinas.FirstOrDefaultAsync(m => m.IdMaquina == id);
            if (maquina == null) return NotFound();
            return View(maquina);
        }

        // ── DELETE POST ──────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var maquina = await _context.Maquinas.FirstOrDefaultAsync(m => m.IdMaquina == id);
            if (maquina == null) return NotFound();

            try
            {
                _context.Maquinas.Remove(maquina);
                await _context.SaveChangesAsync();
                await _auditoriaService.RegistrarAsync("Eliminar", "Maquina", id,
                    $"Eliminó máquina {maquina.NumeroMaquina}");
                TempData["Success"] = $"Máquina {maquina.NumeroMaquina} eliminada.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar esta máquina porque tiene registros asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        // ── HELPER: Registrar log ────────────────────────────────
        private async Task RegistrarLog(int idMaquina, string tipoEvento,
            string? valorAnterior, string? valorNuevo, string? observaciones)
        {
            var idStr  = HttpContext.Session.GetString("UsuarioId");
            var nombre = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            _context.MaquinaLogs.Add(new MaquinaLog
            {
                IdMaquina     = idMaquina,
                IdUsuario     = idUsuario,
                NombreUsuario = nombre,
                TipoEvento    = tipoEvento,
                ValorAnterior = valorAnterior,
                ValorNuevo    = valorNuevo,
                Observaciones = observaciones,
                FechaHora     = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }
}
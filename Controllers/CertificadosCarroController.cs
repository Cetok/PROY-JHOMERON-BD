using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class CertificadosCarroController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public CertificadosCarroController(
            AppDbContext context,
            AuditoriaService auditoriaService,
            NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ════════════════════════════════════════════════════════════
        //  HABILITACIÓN VEHICULAR ESPECIAL
        // ════════════════════════════════════════════════════════════

        // GET: /CertificadosCarro/CrearHabilitacion?idCarro=5
        public async Task<IActionResult> CrearHabilitacion(int idCarro)
        {
            var carro = await _context.Carros.FindAsync(idCarro);
            if (carro == null) return NotFound();

            // Solo Revisiones Técnicas activas
            var modalidades = await _context.Modalidades
                .Where(m => m.Estado == "Activo")
                .OrderBy(m => m.TipoModalidad)
                .ToListAsync();

            ViewBag.Carro      = carro;
            ViewBag.Modalidades = new SelectList(modalidades, "IdModalidad", "TipoModalidad");

            return View(new HabilitacionVehicular { IdCarro = idCarro });
        }

        // POST: /CertificadosCarro/CrearHabilitacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearHabilitacion(HabilitacionVehicular hab)
        {
            ModelState.Remove("Carro");
            ModelState.Remove("Modalidad");

            if (!ModelState.IsValid)
            {
                var carro      = await _context.Carros.FindAsync(hab.IdCarro);
                var modalidades = await _context.Modalidades
                    .Where(m => m.Estado == "Activo").OrderBy(m => m.TipoModalidad).ToListAsync();
                ViewBag.Carro      = carro;
                ViewBag.Modalidades = new SelectList(modalidades, "IdModalidad", "TipoModalidad", hab.IdModalidad);
                return View(hab);
            }

            // Desactivar el anterior vigente para este carro
            var anteriores = await _context.HabilitacionesVehiculares
                .Where(h => h.IdCarro == hab.IdCarro && h.EsVigente)
                .ToListAsync();
            foreach (var a in anteriores) a.EsVigente = false;

            hab.EsVigente     = true;
            hab.FechaRegistro = DateTime.Now;

            _context.HabilitacionesVehiculares.Add(hab);
            await _context.SaveChangesAsync();

            var carroDb = await _context.Carros.FindAsync(hab.IdCarro);
            await _auditoriaService.RegistrarAsync("Crear", "HabilitacionVehicular", hab.IdHabilitacion,
                $"Registró habilitación vehicular [{hab.Codigo}] para carro {carroDb?.Placa}");
            await _notifService.NotificarAccionAsync("Creacion", "Habilitación Vehicular",
                $"Nueva habilitación [{hab.Codigo}] registrada para {carroDb?.Placa}",
                $"/Carros/Details/{hab.IdCarro}",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int uid1) ? uid1 : null);

            TempData["Success"] = $"Habilitación vehicular [{hab.Codigo}] registrada correctamente.";
            return RedirectToAction("Details", "Carros", new { id = hab.IdCarro });
        }

        // GET: /CertificadosCarro/EditarHabilitacion/5
        public async Task<IActionResult> EditarHabilitacion(int id)
        {
            var hab = await _context.HabilitacionesVehiculares
                .Include(h => h.Carro)
                .Include(h => h.Modalidad)
                .FirstOrDefaultAsync(h => h.IdHabilitacion == id);
            if (hab == null) return NotFound();

            var modalidades = await _context.Modalidades
                .Where(m => m.Estado == "Activo").OrderBy(m => m.TipoModalidad).ToListAsync();
            ViewBag.Modalidades = new SelectList(modalidades, "IdModalidad", "TipoModalidad", hab.IdModalidad);
            ViewBag.Carro       = hab.Carro;

            return View(hab);
        }

        // POST: /CertificadosCarro/EditarHabilitacion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarHabilitacion(int id, HabilitacionVehicular hab)
        {
            if (id != hab.IdHabilitacion) return NotFound();
            ModelState.Remove("Carro");
            ModelState.Remove("Modalidad");

            if (!ModelState.IsValid)
            {
                var carro       = await _context.Carros.FindAsync(hab.IdCarro);
                var modalidades = await _context.Modalidades
                    .Where(m => m.Estado == "Activo").OrderBy(m => m.TipoModalidad).ToListAsync();
                ViewBag.Carro       = carro;
                ViewBag.Modalidades = new SelectList(modalidades, "IdModalidad", "TipoModalidad", hab.IdModalidad);
                return View(hab);
            }

            try
            {
                _context.Update(hab);
                await _context.SaveChangesAsync();

                var carroDb = await _context.Carros.FindAsync(hab.IdCarro);
                await _auditoriaService.RegistrarAsync("Editar", "HabilitacionVehicular", id,
                    $"Editó habilitación [{hab.Codigo}] de carro {carroDb?.Placa}");

                TempData["Success"] = "Habilitación actualizada correctamente.";
                return RedirectToAction("Details", "Carros", new { id = hab.IdCarro });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.HabilitacionesVehiculares.AnyAsync(h => h.IdHabilitacion == id))
                    return NotFound();
                throw;
            }
        }

        // POST: /CertificadosCarro/EliminarHabilitacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarHabilitacion(int idHabilitacion, int idCarro)
        {
            var hab = await _context.HabilitacionesVehiculares.FindAsync(idHabilitacion);
            if (hab == null) return NotFound();

            _context.HabilitacionesVehiculares.Remove(hab);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Eliminar", "HabilitacionVehicular", idHabilitacion,
                $"Eliminó habilitación [{hab.Codigo}] del carro #{idCarro}");

            TempData["Success"] = "Habilitación eliminada.";
            return RedirectToAction("Details", "Carros", new { id = idCarro });
        }

        // ════════════════════════════════════════════════════════════
        //  LUNA POLARIZADA
        // ════════════════════════════════════════════════════════════

        // GET: /CertificadosCarro/CrearLuna?idCarro=5
        public async Task<IActionResult> CrearLuna(int idCarro)
        {
            var carro = await _context.Carros.FindAsync(idCarro);
            if (carro == null) return NotFound();

            bool tieneAnterior = await _context.LunasPolarizadas
                .AnyAsync(l => l.IdCarro == idCarro);

            ViewBag.Carro          = carro;
            ViewBag.TieneAnterior  = tieneAnterior;

            return View(new LunaPolarizada { IdCarro = idCarro });
        }

        // POST: /CertificadosCarro/CrearLuna
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearLuna(LunaPolarizada luna)
        {
            ModelState.Remove("Carro");

            bool tieneAnterior = await _context.LunasPolarizadas
                .AnyAsync(l => l.IdCarro == luna.IdCarro);

            // Si ya existe uno anterior, el comentario es obligatorio
            if (tieneAnterior && string.IsNullOrWhiteSpace(luna.Comentario))
                ModelState.AddModelError("Comentario",
                    "Debes indicar el motivo por el que se emite un nuevo certificado.");

            if (!ModelState.IsValid)
            {
                ViewBag.Carro         = await _context.Carros.FindAsync(luna.IdCarro);
                ViewBag.TieneAnterior = tieneAnterior;
                return View(luna);
            }

            // Desactivar el anterior vigente
            var anteriores = await _context.LunasPolarizadas
                .Where(l => l.IdCarro == luna.IdCarro && l.EsVigente)
                .ToListAsync();
            foreach (var a in anteriores) a.EsVigente = false;

            luna.EsVigente     = true;
            luna.FechaRegistro = DateTime.Now;

            _context.LunasPolarizadas.Add(luna);
            await _context.SaveChangesAsync();

            var carroDb = await _context.Carros.FindAsync(luna.IdCarro);
            await _auditoriaService.RegistrarAsync("Crear", "LunaPolarizada", luna.IdLuna,
                $"Registró luna polarizada para carro {carroDb?.Placa}");
            await _notifService.NotificarAccionAsync("Creacion", "Luna Polarizada",
                $"Nueva luna polarizada registrada para {carroDb?.Placa}",
                $"/Carros/Details/{luna.IdCarro}",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int uid2) ? uid2 : null);

            TempData["Success"] = "Luna polarizada registrada correctamente.";
            return RedirectToAction("Details", "Carros", new { id = luna.IdCarro });
        }

        // GET: /CertificadosCarro/EditarLuna/5
        public async Task<IActionResult> EditarLuna(int id)
        {
            var luna = await _context.LunasPolarizadas
                .Include(l => l.Carro)
                .FirstOrDefaultAsync(l => l.IdLuna == id);
            if (luna == null) return NotFound();

            ViewBag.Carro = luna.Carro;
            return View(luna);
        }

        // POST: /CertificadosCarro/EditarLuna/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarLuna(int id, LunaPolarizada luna)
        {
            if (id != luna.IdLuna) return NotFound();
            ModelState.Remove("Carro");

            if (!ModelState.IsValid)
            {
                ViewBag.Carro = await _context.Carros.FindAsync(luna.IdCarro);
                return View(luna);
            }

            try
            {
                _context.Update(luna);
                await _context.SaveChangesAsync();

                var carroDb = await _context.Carros.FindAsync(luna.IdCarro);
                await _auditoriaService.RegistrarAsync("Editar", "LunaPolarizada", id,
                    $"Editó luna polarizada del carro {carroDb?.Placa}");

                TempData["Success"] = "Luna polarizada actualizada correctamente.";
                return RedirectToAction("Details", "Carros", new { id = luna.IdCarro });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.LunasPolarizadas.AnyAsync(l => l.IdLuna == id))
                    return NotFound();
                throw;
            }
        }

        // POST: /CertificadosCarro/EliminarLuna
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarLuna(int idLuna, int idCarro)
        {
            var luna = await _context.LunasPolarizadas.FindAsync(idLuna);
            if (luna == null) return NotFound();

            _context.LunasPolarizadas.Remove(luna);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Eliminar", "LunaPolarizada", idLuna,
                $"Eliminó luna polarizada del carro #{idCarro}");

            TempData["Success"] = "Registro de luna polarizada eliminado.";
            return RedirectToAction("Details", "Carros", new { id = idCarro });
        }
    }
}
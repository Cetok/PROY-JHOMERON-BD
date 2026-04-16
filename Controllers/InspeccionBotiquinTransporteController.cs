using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class InspeccionBotiquinTransporteController : Controller
    {
        private readonly AppDbContext     _context;
        private readonly AuditoriaService _auditoriaService;

        // ── Lista fija de elementos del botiquín ─────────────────
        private static readonly List<string> _elementos = new()
        {
            "Paquete de Guantes quirúrgicos N° 07",
            "Frasco de alcohol 70° de 120 ml",
            "Frasco de agua oxigenada Mediano 120ml",
            "Paquete de algodón 50g",
            "Unidades de gasas estériles de 10cm x 10cm",
            "Paquetes de apósitos 10 x 10 cm",
            "Rollo de esparadrapo 2.5 cm x 5 m",
            "Rollo de Venda elástica 4pulg x 5 yardas",
            "Rollo de Venda elástica 8pulg x 5 yardas",
            "Frasco de jabón antibacterial líquido",
            "Tijera de punta roma",
            "Curitas",
            "Gotas para ojos",
        };

        public InspeccionBotiquinTransporteController(AppDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        // ── CREAR GET ────────────────────────────────────────────
        public async Task<IActionResult> Crear(int idCarro)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            bool yaExiste = await _context.InspeccionBotiquinTransportes
                .AnyAsync(i => i.IdCarro == idCarro && i.FechaInspeccion == hoy);

            if (yaExiste)
            {
                TempData["Error"] = "Ya existe una inspección de botiquín registrada hoy para este vehículo.";
                return RedirectToAction("Details", "Carros", new { id = idCarro });
            }

            ViewBag.Carro     = carro;
            ViewBag.Elementos = _elementos;
            return View();
        }

        // ── CREAR POST ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int idCarro, string numeroBotiquin,
            bool ubicadoEnSuLugar, bool localizadoVisible,
            bool libreDeObstaculos, bool senalizado,
            string inspeccionadoPor, string firmaBase64)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            bool yaExiste = await _context.InspeccionBotiquinTransportes
                .AnyAsync(i => i.IdCarro == idCarro && i.FechaInspeccion == hoy);

            if (yaExiste)
            {
                TempData["Error"] = "Ya existe una inspección de botiquín registrada hoy para este vehículo.";
                return RedirectToAction("Details", "Carros", new { id = idCarro });
            }

            if (string.IsNullOrWhiteSpace(firmaBase64))
            {
                TempData["Error"] = "La firma es obligatoria.";
                ViewBag.Carro     = carro;
                ViewBag.Elementos = _elementos;
                return View();
            }

            // Validar que todos los ítems tengan cantidad y fecha de vencimiento
            for (int i = 0; i < _elementos.Count; i++)
            {
                string? cant  = Request.Form[$"cantidad_{i}"].FirstOrDefault();
                string? fVenc = Request.Form[$"fvenc_{i}"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(cant) || !int.TryParse(cant, out _))
                {
                    TempData["Error"] = $"La cantidad del elemento \"{_elementos[i]}\" es obligatoria.";
                    ViewBag.Carro     = carro;
                    ViewBag.Elementos = _elementos;
                    return View();
                }
                if (string.IsNullOrWhiteSpace(fVenc) || !DateOnly.TryParse(fVenc, out _))
                {
                    TempData["Error"] = $"La fecha de vencimiento de \"{_elementos[i]}\" es obligatoria.";
                    ViewBag.Carro     = carro;
                    ViewBag.Elementos = _elementos;
                    return View();
                }
            }

            var idStr      = HttpContext.Session.GetString("UsuarioId");
            var nomUsuario = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            var inspeccion = new InspeccionBotiquinTransporte
            {
                IdCarro           = idCarro,
                FechaInspeccion   = hoy,
                NumeroBotiquin    = numeroBotiquin.Trim(),
                UbicadoEnSuLugar  = ubicadoEnSuLugar,
                LocalizadoVisible = localizadoVisible,
                LibreDeObstaculos = libreDeObstaculos,
                Senalizado        = senalizado,
                InspeccionadoPor  = inspeccionadoPor.Trim(),
                FirmaBase64       = firmaBase64,
                IdUsuario         = idUsuario,
                NombreUsuario     = nomUsuario,
                FechaRegistro     = DateTime.Now
            };

            // Leer ítems
            for (int i = 0; i < _elementos.Count; i++)
            {
                string? valSe  = Request.Form[$"seencuentra_{i}"].FirstOrDefault();
                int.TryParse(Request.Form[$"cantidad_{i}"].FirstOrDefault(), out int cantidad);
                DateOnly.TryParse(Request.Form[$"fvenc_{i}"].FirstOrDefault(), out DateOnly fVenc);
                string? obs = Request.Form[$"obs_{i}"].FirstOrDefault();

                inspeccion.Items.Add(new InspeccionBotiquinTransporteItem
                {
                    Elemento         = _elementos[i],
                    SeEncuentra      = valSe == "si",
                    Cantidad         = cantidad,
                    FechaVencimiento = fVenc,
                    Observaciones    = string.IsNullOrWhiteSpace(obs) ? null : obs.Trim()
                });
            }

            _context.InspeccionBotiquinTransportes.Add(inspeccion);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Crear", "InspeccionBotiquinTransporte",
                inspeccion.IdInspeccion,
                $"Registró inspección de botiquín para vehículo #{idCarro} — {carro.Placa}");

            TempData["Success"] = "Inspección de botiquín registrada correctamente.";
            return RedirectToAction(nameof(Historial), new { idCarro });
        }

        // ── HISTORIAL ────────────────────────────────────────────
        public async Task<IActionResult> Historial(int idCarro)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var lista = await _context.InspeccionBotiquinTransportes
                .Include(i => i.Items)
                .Where(i => i.IdCarro == idCarro)
                .OrderByDescending(i => i.FechaInspeccion)
                .ToListAsync();

            ViewBag.Carro = carro;
            return View(lista);
        }

        // ── VER DETALLE ──────────────────────────────────────────
        public async Task<IActionResult> Ver(int id)
        {
            var inspeccion = await _context.InspeccionBotiquinTransportes
                .Include(i => i.Items)
                .Include(i => i.Carro)
                .FirstOrDefaultAsync(i => i.IdInspeccion == id);

            if (inspeccion == null) return NotFound();
            return View(inspeccion);
        }

        // ── EDITAR GET ───────────────────────────────────────────
        public async Task<IActionResult> Editar(int id)
        {
            var inspeccion = await _context.InspeccionBotiquinTransportes
                .Include(i => i.Items)
                .Include(i => i.Carro)
                .FirstOrDefaultAsync(i => i.IdInspeccion == id);

            if (inspeccion == null) return NotFound();

            var rolActual = HttpContext.Session.GetString("UsuarioRol") ?? "";
            var idActual  = HttpContext.Session.GetString("UsuarioId")  ?? "";
            if (rolActual != "Admin" && inspeccion.IdUsuario?.ToString() != idActual)
            {
                TempData["Error"] = "No tienes permiso para editar esta inspección.";
                return RedirectToAction(nameof(Ver), new { id });
            }

            if (inspeccion.FueEditado)
            {
                TempData["Warning"] = "Esta inspección ya fue editada y no puede modificarse nuevamente.";
                return RedirectToAction(nameof(Ver), new { id });
            }

            ViewBag.Carro     = inspeccion.Carro;
            ViewBag.Elementos = _elementos;
            return View("Crear", inspeccion);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string numeroBotiquin,
            bool ubicadoEnSuLugar, bool localizadoVisible,
            bool libreDeObstaculos, bool senalizado,
            string inspeccionadoPor, string firmaBase64)
        {
            var inspeccion = await _context.InspeccionBotiquinTransportes
                .Include(i => i.Items)
                .Include(i => i.Carro)
                .FirstOrDefaultAsync(i => i.IdInspeccion == id);

            if (inspeccion == null) return NotFound();

            var rolActual = HttpContext.Session.GetString("UsuarioRol") ?? "";
            var idActual  = HttpContext.Session.GetString("UsuarioId")  ?? "";
            if (rolActual != "Admin" && inspeccion.IdUsuario?.ToString() != idActual)
            {
                TempData["Error"] = "No tienes permiso para editar esta inspección.";
                return RedirectToAction(nameof(Ver), new { id });
            }

            if (inspeccion.FueEditado)
            {
                TempData["Warning"] = "Esta inspección ya fue editada y no puede modificarse nuevamente.";
                return RedirectToAction(nameof(Ver), new { id });
            }

            inspeccion.NumeroBotiquin    = numeroBotiquin.Trim();
            inspeccion.UbicadoEnSuLugar  = ubicadoEnSuLugar;
            inspeccion.LocalizadoVisible = localizadoVisible;
            inspeccion.LibreDeObstaculos = libreDeObstaculos;
            inspeccion.Senalizado        = senalizado;
            inspeccion.InspeccionadoPor  = inspeccionadoPor.Trim();
            if (!string.IsNullOrWhiteSpace(firmaBase64))
                inspeccion.FirmaBase64 = firmaBase64;
            inspeccion.FueEditado = true;

            // Usar Request.Form con los mismos nombres que el Crear
            var items = inspeccion.Items.OrderBy(i => i.IdItem).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                string? valSe = Request.Form[$"seencuentra_{i}"].FirstOrDefault();
                int.TryParse(Request.Form[$"cantidad_{i}"].FirstOrDefault(), out int cant);
                DateOnly.TryParse(Request.Form[$"fvenc_{i}"].FirstOrDefault(), out DateOnly fVenc);
                string? obs = Request.Form[$"obs_{i}"].FirstOrDefault();

                items[i].SeEncuentra   = valSe == "si";
                items[i].Cantidad      = cant;
                if (fVenc != default) items[i].FechaVencimiento = fVenc;
                items[i].Observaciones = string.IsNullOrWhiteSpace(obs) ? null : obs.Trim();
            }

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Editar", "InspeccionBotiquinTransporte", id,
                $"Editó inspección botiquín transporte #{id} — {inspeccion.Carro?.Placa}");

            TempData["Success"] = "Inspección actualizada. Ya no podrá editarse nuevamente.";
            return RedirectToAction(nameof(Ver), new { id });
        }
    }
}
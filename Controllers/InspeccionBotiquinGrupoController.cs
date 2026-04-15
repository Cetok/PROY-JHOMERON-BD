using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class InspeccionBotiquinGrupoController : Controller
    {
        private readonly AppDbContext     _context;
        private readonly AuditoriaService _auditoriaService;

        // ── Grupos con elementos DISTINTOS (Materia Prima, Despacho, Administrativos) ──
        private static readonly List<string> _elementosMateriaPrimaDespachoAdmin = new()
        {
            "02 Paquetes de Guantes quirúrgicos N° 07",
            "01 Frasco de yodopovidona 120 ml solución antiséptico",
            "01 Frasco de agua oxigenada Mediano 120ml",
            "01 Frasco de alcohol mediano 250 ml",
            "5 Paquetes de gasas estériles de 10cm x 10cm",
            "02 Paquetes de apósitos",
            "01 Rollo de esparadrapo 5cm x 4.5 cm",
            "01 Rollo de Venda elástica 4pulg x 5 yardas",
            "01 Rollo de Venda elástica 6pulg x 5 yardas",
            "01 Paquete de algodón 100g",
            "05 Paletas baja lengua",
            "01 Frasco de jabón antibacterial líquido",
            "01 Tijera de punta roma",
            "10 Curitas",
            "01 Pinza",
            "01 Sulfacrem",
            "01 Hirudoid",
            "01 Aceptil Violeta 20 ml",
        };

        // ── Grupos con elementos ESTÁNDAR (Temple y Masilla, Resina, Planificación,
        //    Fiscalizados, Producción, Productos Terminados) ──────────────────────────
        private static readonly List<string> _elementosEstandar = new()
        {
            "02 Paquetes de Guantes quirúrgicos N° 07",
            "01 Frasco de yodopovidona 120 ml solución antiséptico",
            "01 Frasco de agua oxigenada Mediano 120ml",
            "01 Frasco de alcohol mediano 250 ml",
            "5 Paquetes de gasas estériles de 10cm x 10cm",
            "02 Paquetes de apósitos",
            "01 Rollo de esparadrapo 5cm x 4.5 cm",
            "01 Rollo de Venda elástica 4pulg x 5 yardas",
            "01 Rollo de Venda elástica 6pulg x 5 yardas",
            "01 Paquete de algodón 100g",
            "05 Paletas baja lengua",
            "01 Frasco de jabón antibacterial líquido",
            "01 Tijera de punta roma",
            "10 Curitas",
            "01 Pinza",
        };

        // Áreas con lista diferenciada
        private static readonly HashSet<string> _areasEspeciales = new(StringComparer.OrdinalIgnoreCase)
        {
            "Materia Prima", "Despacho", "Administrativos"
        };

        public InspeccionBotiquinGrupoController(AppDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        // ── Devuelve la lista de elementos según el nombre del grupo ─
        private List<string> ObtenerElementos(string nombreGrupo)
        {
            return _areasEspeciales.Contains(nombreGrupo)
                ? _elementosMateriaPrimaDespachoAdmin
                : _elementosEstandar;
        }

        // ── CREAR GET ────────────────────────────────────────────
        public async Task<IActionResult> Crear(int idGrupo)
        {
            var grupo = await _context.Grupos.FirstOrDefaultAsync(g => g.idGrupo == idGrupo);
            if (grupo == null) return NotFound();

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            bool yaExiste = await _context.InspeccionBotiquinGrupos
                .AnyAsync(i => i.IdGrupo == idGrupo && i.FechaInspeccion == hoy);

            if (yaExiste)
            {
                TempData["Error"] = "Ya existe una inspección de botiquín registrada hoy para este grupo.";
                return RedirectToAction("Details", "Grupos", new { id = idGrupo });
            }

            ViewBag.Grupo     = grupo;
            ViewBag.Elementos = ObtenerElementos(grupo.area ?? "");
            return View();
        }

        // ── CREAR POST ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int idGrupo, string numeroBotiquin,
            string? piso, bool instaladoEnPared, bool localizadoVisible,
            bool libreDeObstaculos, bool senalizado,
            string inspeccionadoPor, string firmaBase64)
        {
            var grupo = await _context.Grupos.FirstOrDefaultAsync(g => g.idGrupo == idGrupo);
            if (grupo == null) return NotFound();

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            bool yaExiste = await _context.InspeccionBotiquinGrupos
                .AnyAsync(i => i.IdGrupo == idGrupo && i.FechaInspeccion == hoy);

            if (yaExiste)
            {
                TempData["Error"] = "Ya existe una inspección de botiquín registrada hoy para este grupo.";
                return RedirectToAction("Details", "Grupos", new { id = idGrupo });
            }

            if (string.IsNullOrWhiteSpace(firmaBase64))
            {
                TempData["Error"] = "La firma es obligatoria.";
                ViewBag.Grupo     = grupo;
                ViewBag.Elementos = ObtenerElementos(grupo.area ?? "");
                return View();
            }

            var elementos = ObtenerElementos(grupo.area ?? "");

            // Validar cantidad y fecha de vencimiento de cada ítem
            for (int i = 0; i < elementos.Count; i++)
            {
                string? cant  = Request.Form[$"cantidad_{i}"].FirstOrDefault();
                string? fVenc = Request.Form[$"fvenc_{i}"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(cant) || !int.TryParse(cant, out _))
                {
                    TempData["Error"] = $"La cantidad del elemento \"{elementos[i]}\" es obligatoria.";
                    ViewBag.Grupo     = grupo;
                    ViewBag.Elementos = elementos;
                    return View();
                }
                if (string.IsNullOrWhiteSpace(fVenc) || !DateOnly.TryParse(fVenc, out _))
                {
                    TempData["Error"] = $"La fecha de vencimiento de \"{elementos[i]}\" es obligatoria.";
                    ViewBag.Grupo     = grupo;
                    ViewBag.Elementos = elementos;
                    return View();
                }
            }

            var idStr      = HttpContext.Session.GetString("UsuarioId");
            var nomUsuario = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            var inspeccion = new InspeccionBotiquinGrupo
            {
                IdGrupo          = idGrupo,
                FechaInspeccion  = hoy,
                NumeroBotiquin   = numeroBotiquin.Trim(),
                Piso             = string.IsNullOrWhiteSpace(piso) ? null : piso.Trim(),
                Area             = grupo.area ?? "",
                InstaladoEnPared = instaladoEnPared,
                LocalizadoVisible = localizadoVisible,
                LibreDeObstaculos = libreDeObstaculos,
                Senalizado       = senalizado,
                InspeccionadoPor = inspeccionadoPor.Trim(),
                FirmaBase64      = firmaBase64,
                IdUsuario        = idUsuario,
                NombreUsuario    = nomUsuario,
                FechaRegistro    = DateTime.Now
            };

            for (int i = 0; i < elementos.Count; i++)
            {
                string? valSe = Request.Form[$"seencuentra_{i}"].FirstOrDefault();
                int.TryParse(Request.Form[$"cantidad_{i}"].FirstOrDefault(), out int cantidad);
                DateOnly.TryParse(Request.Form[$"fvenc_{i}"].FirstOrDefault(), out DateOnly fVenc);
                string? obs = Request.Form[$"obs_{i}"].FirstOrDefault();

                inspeccion.Items.Add(new InspeccionBotiquinGrupoItem
                {
                    Elemento         = elementos[i],
                    SeEncuentra      = valSe == "si",
                    Cantidad         = cantidad,
                    FechaVencimiento = fVenc,
                    Observaciones    = string.IsNullOrWhiteSpace(obs) ? null : obs.Trim()
                });
            }

            _context.InspeccionBotiquinGrupos.Add(inspeccion);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Crear", "InspeccionBotiquinGrupo",
                inspeccion.IdInspeccion,
                $"Registró inspección de botiquín para grupo #{idGrupo} — {grupo.area}");

            TempData["Success"] = "Inspección de botiquín registrada correctamente.";
            return RedirectToAction(nameof(Historial), new { idGrupo });
        }

        // ── HISTORIAL ────────────────────────────────────────────
        public async Task<IActionResult> Historial(int idGrupo)
        {
            var grupo = await _context.Grupos.FirstOrDefaultAsync(g => g.idGrupo == idGrupo);
            if (grupo == null) return NotFound();

            var lista = await _context.InspeccionBotiquinGrupos
                .Include(i => i.Items)
                .Where(i => i.IdGrupo == idGrupo)
                .OrderByDescending(i => i.FechaInspeccion)
                .ToListAsync();

            ViewBag.Grupo = grupo;
            return View(lista);
        }

        // ── VER DETALLE ──────────────────────────────────────────
        public async Task<IActionResult> Ver(int id)
        {
            var inspeccion = await _context.InspeccionBotiquinGrupos
                .Include(i => i.Items)
                .Include(i => i.Grupo)
                .FirstOrDefaultAsync(i => i.IdInspeccion == id);

            if (inspeccion == null) return NotFound();
            return View(inspeccion);
        }

        // ── EDITAR GET ───────────────────────────────────────────
        public async Task<IActionResult> Editar(int id)
        {
            var inspeccion = await _context.InspeccionBotiquinGrupos
                .Include(i => i.Items)
                .Include(i => i.Grupo)
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

            var grupo = inspeccion.Grupo;
            ViewBag.Grupo     = grupo;
            ViewBag.Elementos = ObtenerElementos(grupo?.area ?? "");
            return View("Crear", inspeccion);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string numeroBotiquin,
            string? piso, bool instaladoEnPared, bool localizadoVisible,
            bool libreDeObstaculos, bool senalizado, string inspeccionadoPor,
            string firmaBase64)
        {
            var inspeccion = await _context.InspeccionBotiquinGrupos
                .Include(i => i.Items)
                .Include(i => i.Grupo)
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
            inspeccion.Piso              = piso?.Trim();
            inspeccion.InstaladoEnPared  = instaladoEnPared;
            inspeccion.LocalizadoVisible = localizadoVisible;
            inspeccion.LibreDeObstaculos = libreDeObstaculos;
            inspeccion.Senalizado        = senalizado;
            inspeccion.InspeccionadoPor  = inspeccionadoPor.Trim();
            if (!string.IsNullOrWhiteSpace(firmaBase64))
                inspeccion.FirmaBase64 = firmaBase64;
            inspeccion.FueEditado = true;

            // Usar Request.Form con los mismos nombres que usa el Crear
            var elementos = ObtenerElementos(inspeccion.Grupo?.area ?? "");
            var items = inspeccion.Items.OrderBy(i => i.IdItem).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                string? valSe = Request.Form[$"seencuentra_{i}"].FirstOrDefault();
                int.TryParse(Request.Form[$"cantidad_{i}"].FirstOrDefault(), out int cant);
                DateOnly.TryParse(Request.Form[$"fvenc_{i}"].FirstOrDefault(), out DateOnly fVenc);
                string? obs = Request.Form[$"obs_{i}"].FirstOrDefault();

                items[i].SeEncuentra      = valSe == "si";
                items[i].Cantidad         = cant;
                if (fVenc != default) items[i].FechaVencimiento = fVenc;
                items[i].Observaciones    = string.IsNullOrWhiteSpace(obs) ? null : obs.Trim();
            }

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Editar", "InspeccionBotiquinGrupo", id,
                $"Editó inspección botiquín grupo #{id} — {inspeccion.Grupo?.area}");

            TempData["Success"] = "Inspección actualizada. Ya no podrá editarse nuevamente.";
            return RedirectToAction(nameof(Ver), new { id });
        }
    }
}
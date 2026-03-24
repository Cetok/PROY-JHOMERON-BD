using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class CheckListTransporteController : Controller
    {
        private readonly AppDbContext     _context;
        private readonly AuditoriaService _auditoriaService;

        // ── Estructura fija del checklist (sección → ítems) ─────
        private static readonly List<(int Sec, string NombreSec, string Elemento)> _estructura = new()
        {
            (1, "VISTA DEL EXTERIOR EQUIPO", "Buen estado del sistema de frenos"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Las luminarias cuentan con protección."),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Los vidrios son templados o en su defecto cuentan con láminas de seguridad."),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Fuga de aire o líquidos"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Neumáticos delanteros en buen estado"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Neumáticos traseros en buen estado"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Ventanas/vidrios en buen estado (Sin rajaduras)"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Techos en buen estado"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Luces frontales en buen estado"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Luces traseras en buen estado"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Espejos retrovisores en buen estado"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Cuentan con manijas de puerta"),
            (1, "VISTA DEL EXTERIOR EQUIPO", "Letrero de identificación visible"),

            (2, "ESPECÍFICO", "Cinturón de seguridad delanteros y traseros"),
            (2, "ESPECÍFICO", "Bocina en buen estado"),
            (2, "ESPECÍFICO", "Alarma de retroceso en buen estado"),
            (2, "ESPECÍFICO", "Soga en buen estado"),
            (2, "ESPECÍFICO", "Manija en buen estado"),
            (2, "ESPECÍFICO", "Cuenta con taco"),
            (2, "ESPECÍFICO", "Cuenta con cono de seguridad"),
            (2, "ESPECÍFICO", "Cuenta con gata"),

            (3, "PELIGROS DE SEGURIDAD", "Los extintores son los adecuados al tipo de peligros en la que está sujeto el vehículo"),
            (3, "PELIGROS DE SEGURIDAD", "Extintores de carga vigente."),
            (3, "PELIGROS DE SEGURIDAD", "Extintores libre de obstáculos."),
            (3, "PELIGROS DE SEGURIDAD", "Existe señalización de extintores."),
            (3, "PELIGROS DE SEGURIDAD", "Cuenta con su plan de contingencia"),
            (3, "PELIGROS DE SEGURIDAD", "Botiquines en perfecto estado"),
            (3, "PELIGROS DE SEGURIDAD", "Se tiene la lista de teléfonos de emergencia a la mano."),

            (4, "EQUIPO DE PROTECCIÓN PERSONAL", "Cuentan con lentes de protección solar"),
            (4, "EQUIPO DE PROTECCIÓN PERSONAL", "Guantes de seguridad en buen estado"),
            (4, "EQUIPO DE PROTECCIÓN PERSONAL", "Cuentan con faja de seguridad"),
            (4, "EQUIPO DE PROTECCIÓN PERSONAL", "Casco de seguridad en buen estado"),

            (5, "DOCUMENTOS", "Permiso de circulación"),
            (5, "DOCUMENTOS", "Revisión técnica"),
            (5, "DOCUMENTOS", "G.P.S"),

            (6, "ACTOS SUBESTÁNDAR", "Los trabajadores cumplen con las normas de seguridad de su actividad."),
            (6, "ACTOS SUBESTÁNDAR", "Los trabajadores conocen los peligros a los que están expuestos."),
            (6, "ACTOS SUBESTÁNDAR", "El personal tiene claro qué hacer en caso de un incidente, Accidentes de trabajo y Enfermedad Laboral."),
            (6, "ACTOS SUBESTÁNDAR", "Los trabajadores conocen la Política de Seguridad y Salud en el Trabajo."),
        };

        public CheckListTransporteController(AppDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        // ── CREAR (GET) ──────────────────────────────────────────
        public async Task<IActionResult> Crear(int idCarro)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            // Solo un checklist por día por carro
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            bool yaExiste = await _context.CheckListTransportes
                .AnyAsync(cl => cl.IdCarro == idCarro && cl.FechaInspeccion == hoy);

            if (yaExiste)
            {
                TempData["Error"] = "Ya existe un check list registrado hoy para este vehículo.";
                return RedirectToAction("Details", "Carros", new { id = idCarro });
            }

            ViewBag.Carro     = carro;
            ViewBag.Estructura = _estructura;
            ViewBag.Hora      = DateTime.Now.ToString("HH:mm");
            return View();
        }

        // ── CREAR (POST) ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int idCarro, string nombreResponsable,
            string firmaBase64, string? observacionesGenerales,
            List<bool?> cumple, List<string?> observacion)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            // Validar duplicado (por si envían dos veces)
            bool yaExiste = await _context.CheckListTransportes
                .AnyAsync(cl => cl.IdCarro == idCarro && cl.FechaInspeccion == hoy);
            if (yaExiste)
            {
                TempData["Error"] = "Ya existe un check list registrado hoy para este vehículo.";
                return RedirectToAction("Details", "Carros", new { id = idCarro });
            }

            if (string.IsNullOrWhiteSpace(firmaBase64))
            {
                TempData["Error"] = "La firma del responsable es obligatoria.";
                ViewBag.Carro      = carro;
                ViewBag.Estructura = _estructura;
                ViewBag.Hora       = DateTime.Now.ToString("HH:mm");
                return View();
            }

            var idStr      = HttpContext.Session.GetString("UsuarioId");
            var nomUsuario = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            var checkList = new CheckListTransporte
            {
                IdCarro                = idCarro,
                FechaInspeccion        = hoy,
                HoraInspeccion         = TimeOnly.FromDateTime(DateTime.Now),
                SedeArea               = "Transporte",
                NombreResponsable      = nombreResponsable.Trim(),
                FirmaBase64            = firmaBase64,
                ObservacionesGenerales = observacionesGenerales?.Trim(),
                IdUsuario              = idUsuario,
                NombreUsuario          = nomUsuario,
                FechaRegistro          = DateTime.Now
            };

            // Construir ítems
            for (int i = 0; i < _estructura.Count; i++)
            {
                var (sec, nomSec, elem) = _estructura[i];
                checkList.Items.Add(new CheckListTransporteItem
                {
                    Seccion      = sec,
                    NombreSeccion = nomSec,
                    Elemento     = elem,
                    Cumple       = i < cumple.Count ? cumple[i] : null,
                    Observacion  = i < observacion.Count ? observacion[i]?.Trim() : null
                });
            }

            _context.CheckListTransportes.Add(checkList);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Crear", "CheckListTransporte", checkList.IdCheckList,
                $"Registró check list transporte para vehículo #{idCarro} — {carro.Placa}");

            TempData["Success"] = "Check List registrado correctamente.";
            return RedirectToAction(nameof(Historial), new { idCarro });
        }

        // ── HISTORIAL (lista de checklists del carro) ────────────
        public async Task<IActionResult> Historial(int idCarro)
        {
            var carro = await _context.Carros.FirstOrDefaultAsync(c => c.IdCarro == idCarro);
            if (carro == null) return NotFound();

            var lista = await _context.CheckListTransportes
                .Include(cl => cl.Items)
                .Where(cl => cl.IdCarro == idCarro)
                .OrderByDescending(cl => cl.FechaInspeccion)
                .ToListAsync();

            ViewBag.Carro = carro;
            return View(lista);
        }

        // ── VER DETALLE de un checklist ──────────────────────────
        public async Task<IActionResult> Ver(int id)
        {
            var checkList = await _context.CheckListTransportes
                .Include(cl => cl.Items)
                .Include(cl => cl.Carro)
                .FirstOrDefaultAsync(cl => cl.IdCheckList == id);

            if (checkList == null) return NotFound();

            ViewBag.Estructura = _estructura
                .Select(e => e.Sec).Distinct().OrderBy(s => s).ToList();

            return View(checkList);
        }
    }
}
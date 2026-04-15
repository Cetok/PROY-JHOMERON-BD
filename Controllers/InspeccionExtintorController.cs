using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class InspeccionExtintorController : Controller
    {
        private readonly AppDbContext     _context;
        private readonly AuditoriaService _auditoriaService;

        // ── Lista de observaciones 1-17 ──────────────────────────
        public static readonly Dictionary<int, string> Observaciones = new()
        {
            {  1, "Mal Ubicado" },
            {  2, "Acceso Obstruido" },
            {  3, "Zona y/o Extintor no numerados" },
            {  4, "Pictograma de clase de fuego (NTP 350.021): Carece / ilegible" },
            {  5, "Pictograma de forma de uso: Carece / ilegible" },
            {  6, "Etiqueta de recarga: Carece / ilegible" },
            {  7, "Tipo de carga / Concentración del agente ignífugo: No IDENTIFICA" },
            {  8, "Colgador: Ausente / inadecuado" },
            {  9, "Sin pasador y/o precinto de seguridad" },
            { 10, "Manómetro: Con presión inadecuada / dañada" },
            { 11, "Manija de acarreo / palanca de activación / pistola: Dañada o Ausente" },
            { 12, "Manguera: Dañada / ausente" },
            { 13, "Tobera, pitón o pistola: Dañada / ausente" },
            { 14, "Abrazadera/sujetador de manguera: Inadecuado/ausente" },
            { 15, "Cilindro / botella / cartucho impulsor en mal estado" },
            { 16, "Pintura deteriorada en: Cilindro / botella / cartucho impulsor" },
            { 17, "Cartilla de control" },
        };

        public InspeccionExtintorController(AppDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        // ── CREAR GET ────────────────────────────────────────────
        public async Task<IActionResult> Crear(int idAsesorio)
        {
            var accesorio = await _context.Asesorios
                .FirstOrDefaultAsync(a => a.IdAsesorio == idAsesorio);
            if (accesorio == null) return NotFound();

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            bool yaExiste = await _context.InspeccionExtintores
                .AnyAsync(i => i.IdAsesorio == idAsesorio && i.FechaInspeccion == hoy);

            if (yaExiste)
            {
                TempData["Error"] = "Ya existe una inspección de extintores registrada hoy.";
                return RedirectToAction("Details", "Asesorios", new { id = idAsesorio });
            }

            // Grupos que tienen asignado este extintor
            var gruposConExtintor = await _context.GrupoAsesorios
                .Include(ga => ga.Grupo)
                .Where(ga => ga.IdAsesorio == idAsesorio && ga.TipoExtintor != null)
                .OrderBy(ga => ga.Grupo.area)
                .ToListAsync();

            if (!gruposConExtintor.Any())
            {
                TempData["Error"] = "No hay grupos con este extintor asignado para inspeccionar.";
                return RedirectToAction("Details", "Asesorios", new { id = idAsesorio });
            }

            ViewBag.Accesorio         = accesorio;
            ViewBag.GruposConExtintor = gruposConExtintor;
            ViewBag.Observaciones     = Observaciones;
            return View();
        }

        // ── CREAR POST ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int idAsesorio, string inspeccionadoPor, string firmaBase64)
        {
            var accesorio = await _context.Asesorios
                .FirstOrDefaultAsync(a => a.IdAsesorio == idAsesorio);
            if (accesorio == null) return NotFound();

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            bool yaExiste = await _context.InspeccionExtintores
                .AnyAsync(i => i.IdAsesorio == idAsesorio && i.FechaInspeccion == hoy);

            if (yaExiste)
            {
                TempData["Error"] = "Ya existe una inspección registrada hoy.";
                return RedirectToAction("Details", "Asesorios", new { id = idAsesorio });
            }

            if (string.IsNullOrWhiteSpace(firmaBase64))
            {
                TempData["Error"] = "La firma es obligatoria.";
                return await RecargarVista(idAsesorio, accesorio);
            }

            var gruposConExtintor = await _context.GrupoAsesorios
                .Include(ga => ga.Grupo)
                .Where(ga => ga.IdAsesorio == idAsesorio && ga.TipoExtintor != null)
                .OrderBy(ga => ga.Grupo.area)
                .ToListAsync();

            var idStr      = HttpContext.Session.GetString("UsuarioId");
            var nomUsuario = HttpContext.Session.GetString("UsuarioNombre");
            int? idUsuario = int.TryParse(idStr, out int uid) ? uid : null;

            var inspeccion = new InspeccionExtintor
            {
                IdAsesorio       = idAsesorio,
                FechaInspeccion  = hoy,
                InspeccionadoPor = inspeccionadoPor.Trim(),
                FirmaBase64      = firmaBase64,
                IdUsuario        = idUsuario,
                NombreUsuario    = nomUsuario,
                FechaRegistro    = DateTime.Now
            };

            // Construir filas
            for (int i = 0; i < gruposConExtintor.Count; i++)
            {
                var ga = gruposConExtintor[i];

                // Observaciones marcadas: checkboxes obs_{i}_{num}
                var marcadas = new List<int>();
                for (int n = 1; n <= 17; n++)
                {
                    var val = Request.Form[$"obs_{i}_{n}"].FirstOrDefault();
                    if (val == "on" || val == "true") marcadas.Add(n);
                }
                bool marca18 = Request.Form[$"obs_{i}_18"].FirstOrDefault() == "on"
                            || Request.Form[$"obs_{i}_18"].FirstOrDefault() == "true";
                string? texto18 = Request.Form[$"obs18text_{i}"].FirstOrDefault()?.Trim();

                // Validar: si 18 marcado, texto obligatorio
                if (marca18 && string.IsNullOrWhiteSpace(texto18))
                {
                    TempData["Error"] = $"El campo 'Otros (Indicar)' del grupo '{ga.Grupo?.area}' es obligatorio cuando se marca el ítem 18.";
                    return await RecargarVista(idAsesorio, accesorio);
                }
                if (marca18) marcadas.Add(18);

                inspeccion.Filas.Add(new InspeccionExtintorFila
                {
                    IdGrupo              = ga.IdGrupo,
                    NombreGrupo          = ga.Grupo?.area ?? "",
                    TipoExtintor         = ga.TipoExtintor,
                    PesoExtintor         = ga.PesoExtintor,
                    FechaVencimiento     = ga.FechaVencimientoExtintor,
                    Comentario           = Request.Form[$"comentario_{i}"].FirstOrDefault()?.Trim(),
                    ObservacionesMarcadas = marcadas.Any() ? string.Join(",", marcadas) : null,
                    Observacion18        = marca18 ? texto18 : null
                });
            }

            _context.InspeccionExtintores.Add(inspeccion);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Crear", "InspeccionExtintor",
                inspeccion.IdInspeccion,
                $"Registró inspección de extintores para accesorio #{idAsesorio}");

            TempData["Success"] = "Inspección de extintores registrada correctamente.";
            return RedirectToAction(nameof(Historial), new { idAsesorio });
        }

        // ── HISTORIAL ────────────────────────────────────────────
        public async Task<IActionResult> Historial(int idAsesorio)
        {
            var accesorio = await _context.Asesorios
                .FirstOrDefaultAsync(a => a.IdAsesorio == idAsesorio);
            if (accesorio == null) return NotFound();

            var lista = await _context.InspeccionExtintores
                .Include(i => i.Filas)
                .Where(i => i.IdAsesorio == idAsesorio)
                .OrderByDescending(i => i.FechaInspeccion)
                .ToListAsync();

            ViewBag.Accesorio = accesorio;
            return View(lista);
        }

        // ── VER DETALLE ──────────────────────────────────────────
        public async Task<IActionResult> Ver(int id)
        {
            var inspeccion = await _context.InspeccionExtintores
                .Include(i => i.Filas).ThenInclude(f => f.Grupo)
                .Include(i => i.Asesorio)
                .FirstOrDefaultAsync(i => i.IdInspeccion == id);

            if (inspeccion == null) return NotFound();

            ViewBag.Observaciones = Observaciones;
            return View(inspeccion);
        }

        // ── EDITAR GET ───────────────────────────────────────────
        public async Task<IActionResult> Editar(int id)
        {
            var inspeccion = await _context.InspeccionExtintores
                .Include(i => i.Filas).ThenInclude(f => f.Grupo)
                .Include(i => i.Asesorio)
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

            var gruposConExtintor = await _context.GrupoAsesorios
                .Include(ga => ga.Grupo)
                .Where(ga => ga.IdAsesorio == inspeccion.IdAsesorio && ga.TipoExtintor != null)
                .OrderBy(ga => ga.Grupo.area)
                .ToListAsync();

            ViewBag.Accesorio         = inspeccion.Asesorio;
            ViewBag.GruposConExtintor = gruposConExtintor;
            ViewBag.Observaciones     = Observaciones;
            return View("Crear", inspeccion);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string inspeccionadoPor, string firmaBase64)
        {
            var inspeccion = await _context.InspeccionExtintores
                .Include(i => i.Filas)
                .Include(i => i.Asesorio)
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

            inspeccion.InspeccionadoPor = inspeccionadoPor.Trim();
            if (!string.IsNullOrWhiteSpace(firmaBase64))
                inspeccion.FirmaBase64 = firmaBase64;
            inspeccion.FueEditado = true;

            // Actualizar filas usando Request.Form igual que el Crear
            for (int i = 0; i < inspeccion.Filas.Count; i++)
            {
                var fila = inspeccion.Filas.OrderBy(f => f.IdFila).ElementAtOrDefault(i);
                if (fila == null) continue;

                var marcadas = new List<int>();
                for (int n = 1; n <= 17; n++)
                {
                    var val = Request.Form[$"obs_{i}_{n}"].FirstOrDefault();
                    if (val == "on" || val == "true") marcadas.Add(n);
                }
                bool marca18   = Request.Form[$"obs_{i}_18"].FirstOrDefault() == "on"
                              || Request.Form[$"obs_{i}_18"].FirstOrDefault() == "true";
                string? texto18 = Request.Form[$"obs18text_{i}"].FirstOrDefault()?.Trim();

                fila.ObservacionesMarcadas = marcadas.Any() ? string.Join(",", marcadas) : null;
                fila.Observacion18         = marca18 ? texto18 : null;
                fila.Comentario            = Request.Form[$"comentario_{i}"].FirstOrDefault()?.Trim();
            }

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Editar", "InspeccionExtintor", id,
                $"Editó inspección extintor #{id}");

            TempData["Success"] = "Inspección de extintor actualizada. Ya no podrá editarse nuevamente.";
            return RedirectToAction(nameof(Ver), new { id });
        }

        // ── Helper para recargar vista con datos ─────────────────
        private async Task<IActionResult> RecargarVista(int idAsesorio, Asesorio accesorio)
        {
            var gruposConExtintor = await _context.GrupoAsesorios
                .Include(ga => ga.Grupo)
                .Where(ga => ga.IdAsesorio == idAsesorio && ga.TipoExtintor != null)
                .OrderBy(ga => ga.Grupo.area)
                .ToListAsync();

            ViewBag.Accesorio         = accesorio;
            ViewBag.GruposConExtintor = gruposConExtintor;
            ViewBag.Observaciones     = Observaciones;
            return View();
        }
    }
}
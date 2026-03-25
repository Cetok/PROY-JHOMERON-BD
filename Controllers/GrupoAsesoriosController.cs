using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class GrupoAsesoriosController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public GrupoAsesoriosController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── ASIGNAR GET ──────────────────────────────────────────
        public async Task<IActionResult> Asignar(int idGrupo)
        {
            var grupo = await _context.Grupos.FirstOrDefaultAsync(g => g.idGrupo == idGrupo);
            if (grupo == null) return NotFound();

            var asignados = await _context.GrupoAsesorios
                .Where(ga => ga.IdGrupo == idGrupo)
                .Select(ga => ga.IdAsesorio)
                .ToListAsync();

            var disponibles = await _context.Asesorios
                .Where(a => !asignados.Contains(a.IdAsesorio))
                .OrderBy(a => a.TipoAsesorio)
                .ToListAsync();

            ViewBag.Grupo       = grupo;
            ViewBag.Disponibles = new SelectList(disponibles, "IdAsesorio", "TipoAsesorio");
            ViewBag.AccesoriosJson = System.Text.Json.JsonSerializer.Serialize(
                disponibles.Select(a => new { id = a.IdAsesorio, tipo = a.TipoAsesorio })
            );

            return View(new GrupoAsesorio { IdGrupo = idGrupo });
        }

        // ── ASIGNAR POST ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(GrupoAsesorio vm,
            string? tipoExtintor, string? pesoExtintor, string? fechaVencimientoExtintorStr)
        {
            ModelState.Remove("Grupo");
            ModelState.Remove("Asesorio");

            bool existe = await _context.GrupoAsesorios
                .AnyAsync(ga => ga.IdGrupo == vm.IdGrupo && ga.IdAsesorio == vm.IdAsesorio);

            if (existe)
            {
                TempData["Error"] = "Este accesorio ya está asignado al grupo.";
                return RedirectToAction("Details", "Grupos", new { id = vm.IdGrupo });
            }

            var accesorio   = await _context.Asesorios.FindAsync(vm.IdAsesorio);
            bool esExtintor = accesorio?.TipoAsesorio?.ToLower().Contains("extintor") == true;

            if (esExtintor)
            {
                vm.TipoExtintor             = tipoExtintor;
                vm.PesoExtintor             = pesoExtintor;
                vm.FechaVencimientoExtintor = DateOnly.TryParse(fechaVencimientoExtintorStr, out var fv) ? fv : null;
            }
            else
            {
                vm.TipoExtintor             = null;
                vm.PesoExtintor             = null;
                vm.FechaVencimientoExtintor = null;
            }

            if (ModelState.IsValid)
            {
                _context.Add(vm);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Crear", "GrupoAsesorio", vm.IdAsesorio,
                    $"Asignó accesorio '{accesorio?.TipoAsesorio}' al grupo #{vm.IdGrupo}");

                await _notifService.NotificarAccionAsync("Creacion", "Accesorio",
                    $"Accesorio '{accesorio?.TipoAsesorio}' asignado al grupo",
                    $"/Grupos/Details/{vm.IdGrupo}");

                TempData["Success"] = "Accesorio asignado al grupo correctamente.";
                return RedirectToAction("Details", "Grupos", new { id = vm.IdGrupo });
            }

            var grupo = await _context.Grupos.FirstOrDefaultAsync(g => g.idGrupo == vm.IdGrupo);
            var asignados = await _context.GrupoAsesorios
                .Where(ga => ga.IdGrupo == vm.IdGrupo).Select(ga => ga.IdAsesorio).ToListAsync();
            var disponibles = await _context.Asesorios
                .Where(a => !asignados.Contains(a.IdAsesorio)).OrderBy(a => a.TipoAsesorio).ToListAsync();

            ViewBag.Grupo       = grupo;
            ViewBag.Disponibles = new SelectList(disponibles, "IdAsesorio", "TipoAsesorio", vm.IdAsesorio);
            ViewBag.AccesoriosJson = System.Text.Json.JsonSerializer.Serialize(
                disponibles.Select(a => new { id = a.IdAsesorio, tipo = a.TipoAsesorio })
            );
            return View(vm);
        }

        // ── EDITAR GET ───────────────────────────────────────────
        public async Task<IActionResult> Editar(int idGrupo, int idAsesorio)
        {
            var ga = await _context.GrupoAsesorios
                .Include(x => x.Grupo)
                .Include(x => x.Asesorio)
                .FirstOrDefaultAsync(x => x.IdGrupo == idGrupo && x.IdAsesorio == idAsesorio);

            if (ga == null) return NotFound();

            ViewBag.EsExtintor = ga.Asesorio?.TipoAsesorio?.ToLower().Contains("extintor") == true;
            return View(ga);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int idGrupo, int idAsesorio, GrupoAsesorio vm,
            string? tipoExtintor, string? pesoExtintor, string? fechaVencimientoExtintorStr)
        {
            ModelState.Remove("Grupo");
            ModelState.Remove("Asesorio");

            if (ModelState.IsValid)
            {
                var existing = await _context.GrupoAsesorios
                    .Include(x => x.Asesorio)
                    .FirstOrDefaultAsync(x => x.IdGrupo == idGrupo && x.IdAsesorio == idAsesorio);

                if (existing == null) return NotFound();

                existing.FechaAsignada = vm.FechaAsignada;
                existing.Observaciones = vm.Observaciones;

                bool esExtintor = existing.Asesorio?.TipoAsesorio?.ToLower().Contains("extintor") == true;
                if (esExtintor)
                {
                    existing.TipoExtintor             = tipoExtintor;
                    existing.PesoExtintor             = pesoExtintor;
                    existing.FechaVencimientoExtintor = DateOnly.TryParse(fechaVencimientoExtintorStr, out var fv) ? fv : null;
                }
                else
                {
                    existing.TipoExtintor             = null;
                    existing.PesoExtintor             = null;
                    existing.FechaVencimientoExtintor = null;
                }

                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Editar", "GrupoAsesorio", idAsesorio,
                    $"Editó accesorio '{existing.Asesorio?.TipoAsesorio}' del grupo #{idGrupo}");

                await _notifService.NotificarAccionAsync("Edicion", "Accesorio",
                    $"Accesorio '{existing.Asesorio?.TipoAsesorio}' actualizado en grupo",
                    $"/Grupos/Details/{idGrupo}");

                TempData["Success"] = "Accesorio actualizado.";
                return RedirectToAction("Details", "Grupos", new { id = idGrupo });
            }

            var ga = await _context.GrupoAsesorios
                .Include(x => x.Grupo).Include(x => x.Asesorio)
                .FirstOrDefaultAsync(x => x.IdGrupo == idGrupo && x.IdAsesorio == idAsesorio);

            ViewBag.EsExtintor = ga?.Asesorio?.TipoAsesorio?.ToLower().Contains("extintor") == true;
            return View(vm);
        }

        // ── QUITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(int idGrupo, int idAsesorio)
        {
            var ga = await _context.GrupoAsesorios
                .Include(x => x.Asesorio)
                .FirstOrDefaultAsync(x => x.IdGrupo == idGrupo && x.IdAsesorio == idAsesorio);

            if (ga != null)
            {
                var nombreAcc = ga.Asesorio?.TipoAsesorio;
                _context.GrupoAsesorios.Remove(ga);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync("Eliminar", "GrupoAsesorio", idAsesorio,
                    $"Quitó accesorio '{nombreAcc}' del grupo #{idGrupo}");

                await _notifService.NotificarAccionAsync("Eliminacion", "Accesorio",
                    $"Accesorio '{nombreAcc}' quitado del grupo");

                TempData["Success"] = "Accesorio quitado del grupo.";
            }
            return RedirectToAction("Details", "Grupos", new { id = idGrupo });
        }
    }
}
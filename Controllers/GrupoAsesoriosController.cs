using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class GrupoAsesoriosController : Controller
    {
        private readonly AppDbContext _context;

        public GrupoAsesoriosController(AppDbContext context)
        {
            _context = context;
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

            return View(new GrupoAsesorio { IdGrupo = idGrupo, FechaAsignada = DateTime.Today });
        }

        // ── ASIGNAR POST ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(GrupoAsesorio vm)
        {
            ModelState.Remove("Grupo");
            ModelState.Remove("Asesorio");

            if (ModelState.IsValid)
            {
                bool yaExiste = await _context.GrupoAsesorios
                    .AnyAsync(ga => ga.IdGrupo == vm.IdGrupo && ga.IdAsesorio == vm.IdAsesorio);

                if (yaExiste)
                {
                    TempData["Error"] = "Este accesorio ya está asignado al grupo.";
                    return RedirectToAction("Details", "Grupos", new { id = vm.IdGrupo });
                }

                _context.Add(vm);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Accesorio asignado al grupo correctamente.";
                return RedirectToAction("Details", "Grupos", new { id = vm.IdGrupo });
            }

            var grupo = await _context.Grupos.FirstOrDefaultAsync(g => g.idGrupo == vm.IdGrupo);
            var asignados = await _context.GrupoAsesorios.Where(ga => ga.IdGrupo == vm.IdGrupo).Select(ga => ga.IdAsesorio).ToListAsync();
            var disponibles = await _context.Asesorios.Where(a => !asignados.Contains(a.IdAsesorio)).OrderBy(a => a.TipoAsesorio).ToListAsync();
            ViewBag.Grupo       = grupo;
            ViewBag.Disponibles = new SelectList(disponibles, "IdAsesorio", "TipoAsesorio", vm.IdAsesorio);
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
            return View(ga);
        }

        // ── EDITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int idGrupo, int idAsesorio, GrupoAsesorio vm)
        {
            ModelState.Remove("Grupo");
            ModelState.Remove("Asesorio");

            if (ModelState.IsValid)
            {
                var existing = await _context.GrupoAsesorios
                    .FirstOrDefaultAsync(x => x.IdGrupo == idGrupo && x.IdAsesorio == idAsesorio);

                if (existing == null) return NotFound();

                existing.FechaAsignada = vm.FechaAsignada;
                existing.Observaciones = vm.Observaciones;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Accesorio actualizado.";
                return RedirectToAction("Details", "Grupos", new { id = idGrupo });
            }

            var ga = await _context.GrupoAsesorios
                .Include(x => x.Grupo).Include(x => x.Asesorio)
                .FirstOrDefaultAsync(x => x.IdGrupo == idGrupo && x.IdAsesorio == idAsesorio);
            return View(ga);
        }

        // ── QUITAR POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(int idGrupo, int idAsesorio)
        {
            var ga = await _context.GrupoAsesorios
                .FirstOrDefaultAsync(x => x.IdGrupo == idGrupo && x.IdAsesorio == idAsesorio);

            if (ga != null)
            {
                _context.GrupoAsesorios.Remove(ga);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Accesorio removido del grupo.";
            }

            return RedirectToAction("Details", "Grupos", new { id = idGrupo });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                // ── Totales ──────────────────────────────────────────────
                TotalEmpleados           = await _context.Empleados.CountAsync(),
                EmpleadosActivos         = await _context.Empleados.CountAsync(e => e.estado == "Activo"),
                TotalEquipos             = await _context.Equipos.CountAsync(),
                EquiposAsignados         = await _context.Asignaciones
                                                .Where(a => a.EstadoAsignacion == "Activo")
                                                .Select(a => a.IdEquipo)
                                                .Distinct().CountAsync(),
                TotalCarros              = await _context.Carros.CountAsync(),
                CarrosActivos            = await _context.Carros.CountAsync(c => c.Estado == "Activo"),
                MantenimientosPendientes = await _context.MantenimientosCarros
                                                .CountAsync(m => m.Estado == "Pendiente"),

                // ── Últimas asignaciones ─────────────────────────────────
                UltimasAsignaciones      = await _context.Asignaciones
                                                .Include(a => a.Empleado)
                                                .Include(a => a.Equipo)
                                                .OrderByDescending(a => a.FechaAsignacion)
                                                .Take(6)
                                                .ToListAsync(),

                // ── Mantenimientos pendientes / en proceso ───────────────
                MantenimientosRecientes  = await _context.MantenimientosCarros
                                                .Include(m => m.Carro)
                                                .Include(m => m.TipoMantenimiento)
                                                .Where(m => m.Estado == "Pendiente" || m.Estado == "En proceso")
                                                .OrderBy(m => m.FechaMante)
                                                .Take(6)
                                                .ToListAsync(),
            };

            return View(vm);
        }
    }

    // ── ViewModel interno ─────────────────────────────────────────────
    public class DashboardViewModel
    {
        // Conteos
        public int TotalEmpleados            { get; set; }
        public int EmpleadosActivos          { get; set; }
        public int TotalEquipos              { get; set; }
        public int EquiposAsignados          { get; set; }
        public int TotalCarros               { get; set; }
        public int CarrosActivos             { get; set; }
        public int MantenimientosPendientes  { get; set; }

        // Listas
        public List<Asignacion>         UltimasAsignaciones     { get; set; } = new();
        public List<MantenimientoCarro> MantenimientosRecientes { get; set; } = new();
    }
}


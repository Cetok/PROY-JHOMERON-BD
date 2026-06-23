using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        public HomeController(AppDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var rol      = HttpContext.Session.GetString("UsuarioRol") ?? "";
            var username = HttpContext.Session.GetString("UsuarioUsername") ?? "";

            // danitza siempre va al dashboard admin, sin importar su rol
            if (username.Equals("danitza", StringComparison.OrdinalIgnoreCase))
                return await MostrarDashboardAdmin();

            return rol switch
            {
                "SoporteTI"  => RedirectToAction("Index", "Equipos"),
                "Transporte" => RedirectToAction("Index", "Carros"),
                "Produccion" => RedirectToAction("Index", "Maquinas"),
                "SSOMA"      => RedirectToAction("Index", "Carros"),
                "Logistica"  => RedirectToAction("Index", "Chips"),
                _            => await MostrarDashboardAdmin()   // Admin
            };
        }

        // ── AJAX: movimientos paginados ───────────────────────────
        [HttpGet]
        public async Task<IActionResult> MovimientosData(int pagina = 1)
        {
            const int porPagina  = 10;
            const int maxPaginas = 10;
            const int totalMax   = porPagina * maxPaginas; // máx 100

            var totalReal = await _context.AuditoriaLogs.CountAsync();
            var total     = Math.Min(totalReal, totalMax);

            var movimientos = await _context.AuditoriaLogs
                .OrderByDescending(l => l.FechaHora)
                .Take(totalMax)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .Select(l => new {
                    fecha        = l.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    l.Accion,
                    l.Entidad,
                    l.Descripcion,
                    l.NombreUsuario
                })
                .ToListAsync();

            return Json(new {
                total,
                pagina,
                totalPaginas = (int)Math.Ceiling((double)total / porPagina),
                registros    = movimientos
            });
        }

        private async Task<IActionResult> MostrarDashboardAdmin()
        {
            var vm = new DashboardViewModel
            {
                TotalEmpleados           = await _context.Empleados.CountAsync(),
                EmpleadosActivos         = await _context.Empleados.CountAsync(e => e.estado == "Activo"),
                TotalEquipos             = await _context.Equipos.CountAsync(),
                EquiposAsignados         = await _context.Asignaciones
                    .Where(a => a.EstadoAsignacion == "Activo")
                    .Select(a => a.IdEquipo).Distinct().CountAsync(),
                TotalCarros              = await _context.Carros.CountAsync(),
                CarrosActivos            = await _context.Carros.CountAsync(c => c.Estado == "Activo"),
                MantenimientosPendientes = await _context.MantenimientosCarros.CountAsync(m => m.Estado == "Pendiente"),
                TotalMaquinas            = await _context.Maquinas.CountAsync(),
                MaquinasActivas          = await _context.Maquinas.CountAsync(m => m.Estado == "Activo"),

                UltimasAsignaciones = await _context.Asignaciones
                    .Include(a => a.Empleado).Include(a => a.Equipo)
                    .OrderByDescending(a => a.FechaAsignacion).Take(6).ToListAsync(),

                MantenimientosRecientes = await _context.MantenimientosCarros
                    .Include(m => m.Carro).Include(m => m.TipoMantenimiento).Include(m => m.UsuarioCreador)
                    .Where(m => m.Estado == "Pendiente")
                    .OrderBy(m => m.FechaProgramada).Take(6).ToListAsync(),

                EquiposPorEstado = await _context.Equipos
                    .GroupBy(e => e.estado_equipo)
                    .Select(g => new EstadoCount { Estado = g.Key ?? "—", Total = g.Count() })
                    .ToListAsync(),

                CarrosPorEstado = await _context.Carros
                    .GroupBy(c => c.Estado)
                    .Select(g => new EstadoCount { Estado = g.Key ?? "—", Total = g.Count() })
                    .ToListAsync(),

                MaquinasPorEstado = await _context.Maquinas
                    .GroupBy(m => m.Estado)
                    .Select(g => new EstadoCount { Estado = g.Key ?? "—", Total = g.Count() })
                    .ToListAsync(),
            };
            return View("Index", vm);
        }
    }

    public class DashboardViewModel
    {
        public int TotalEmpleados            { get; set; }
        public int EmpleadosActivos          { get; set; }
        public int TotalEquipos              { get; set; }
        public int EquiposAsignados          { get; set; }
        public int TotalCarros               { get; set; }
        public int CarrosActivos             { get; set; }
        public int MantenimientosPendientes  { get; set; }
        public int TotalMaquinas             { get; set; }
        public int MaquinasActivas           { get; set; }

        public List<Asignacion>         UltimasAsignaciones     { get; set; } = new();
        public List<MantenimientoCarro> MantenimientosRecientes { get; set; } = new();
        public List<AuditoriaLog>       UltimosMovimientos      { get; set; } = new();
        public List<EstadoCount>        EquiposPorEstado        { get; set; } = new();
        public List<EstadoCount>        CarrosPorEstado         { get; set; } = new();
        public List<EstadoCount>        MaquinasPorEstado       { get; set; } = new();
    }

    public class EstadoCount
    {
        public string Estado { get; set; } = "";
        public int    Total  { get; set; }
    }
}
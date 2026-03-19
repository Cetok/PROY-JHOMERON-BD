using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;

namespace PROYJHOME2026.Controllers
{
    public class CarroEstadoLogsController : Controller
    {
        private readonly AppDbContext _context;

        public CarroEstadoLogsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? buscar, string? estadoFiltro, int pagina = 1)
        {
            int porPagina = 15;

            var query = _context.CarroEstadoLogs
                .Include(l => l.Carro)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(l =>
                    l.Carro.Placa.Contains(buscar)   ||
                    l.Motivo.Contains(buscar)          ||
                    (l.NombreUsuario != null && l.NombreUsuario.Contains(buscar)));

            if (!string.IsNullOrWhiteSpace(estadoFiltro))
                query = query.Where(l => l.EstadoNuevo == estadoFiltro);

            int total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.FechaHora)
                .Skip((pagina - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            ViewBag.Buscar       = buscar;
            ViewBag.EstadoFiltro = estadoFiltro;
            ViewBag.Pagina       = pagina;
            ViewBag.Total        = total;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

            return View(logs);
        }
    }
}
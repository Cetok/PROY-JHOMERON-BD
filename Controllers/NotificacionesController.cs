using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class NotificacionesController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly NotificacionService _notifService;

        public NotificacionesController(AppDbContext context, NotificacionService notifService)
        {
            _context      = context;
            _notifService = notifService;
        }

        // ── GET: /Notificaciones/ObtenerNoLeidas ─────────────────
        [HttpGet]
        public async Task<IActionResult> ObtenerNoLeidas()
        {
            var idStr = HttpContext.Session.GetString("UsuarioId");
            if (!int.TryParse(idStr, out int idUsuario))
                return Json(new { contador = 0, notificaciones = new List<object>() });

            var notifs = await _notifService.ObtenerUltimasAsync(idUsuario, 15);

            // Excluir notificaciones internas de tipo "Sistema" (marcadores de email enviado)
            var noLeidas = notifs.Count(n => !n.Leida && n.Tipo != "Sistema");

            var resultado = notifs
                .Where(n => n.Tipo != "Sistema")
                .Select(n => new {
                    id            = n.IdNotificacion,
                    tipo          = n.Tipo,
                    titulo        = n.Titulo,
                    mensaje       = n.Mensaje,
                    url           = n.Url,
                    leida         = n.Leida,
                    fechaCreacion = n.FechaCreacion.ToString("dd/MM/yyyy HH:mm")
                });

            return Json(new { contador = noLeidas, notificaciones = resultado });
        }

        // ── POST: /Notificaciones/MarcarLeida ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            await _notifService.MarcarLeidaAsync(id);
            return Json(new { ok = true });
        }

        // ── POST: /Notificaciones/MarcarTodasLeidas ──────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var idStr = HttpContext.Session.GetString("UsuarioId");
            if (int.TryParse(idStr, out int idUsuario))
                await _notifService.MarcarTodasLeidasAsync(idUsuario);

            return Json(new { ok = true });
        }

        // ── GET: /Notificaciones/Index ───────────────────────────
        public async Task<IActionResult> Index()
        {
            var idStr = HttpContext.Session.GetString("UsuarioId");
            if (!int.TryParse(idStr, out int idUsuario))
                return RedirectToAction("Login", "Auth");

            var notifs = await _context.Notificaciones
                .Where(n => n.IdUsuario == idUsuario && n.Tipo != "Sistema")
                .OrderByDescending(n => n.FechaCreacion)
                .Take(50)
                .ToListAsync();

            await _notifService.MarcarTodasLeidasAsync(idUsuario);

            return View(notifs);
        }
    }
}
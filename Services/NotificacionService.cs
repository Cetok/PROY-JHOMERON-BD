using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Services
{
    public class NotificacionService
    {
        private readonly AppDbContext _context;

        public NotificacionService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Crea una notificación para todos los usuarios activos o para uno específico.</summary>
        public async Task CrearAsync(
            string tipo,
            string titulo,
            string? mensaje,
            string? url = null,
            int? idMante = null,
            int? soloParaUsuario = null)
        {
            var usuariosIds = soloParaUsuario.HasValue
                ? new List<int> { soloParaUsuario.Value }
                : await _context.Usuarios
                    .Where(u => u.activo)
                    .Select(u => u.idUsuario)
                    .ToListAsync();

            foreach (var uid in usuariosIds)
            {
                _context.Notificaciones.Add(new Notificacion
                {
                    IdUsuario     = uid,
                    Tipo          = tipo,
                    Titulo        = titulo,
                    Mensaje       = mensaje,
                    Url           = url,
                    IdMante       = idMante,
                    Leida         = false,
                    FechaCreacion = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>Devuelve el conteo de notificaciones no leídas para un usuario.</summary>
        public async Task<int> ContarNoLeidasAsync(int idUsuario)
        {
            return await _context.Notificaciones
                .CountAsync(n => n.IdUsuario == idUsuario && !n.Leida);
        }

        /// <summary>Devuelve las últimas N notificaciones de un usuario.</summary>
        public async Task<List<Notificacion>> ObtenerUltimasAsync(int idUsuario, int cantidad = 15)
        {
            return await _context.Notificaciones
                .Where(n => n.IdUsuario == idUsuario)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(cantidad)
                .ToListAsync();
        }

        /// <summary>Marca todas las notificaciones de un usuario como leídas.</summary>
        public async Task MarcarTodasLeidasAsync(int idUsuario)
        {
            var noLeidas = await _context.Notificaciones
                .Where(n => n.IdUsuario == idUsuario && !n.Leida)
                .ToListAsync();

            foreach (var n in noLeidas) n.Leida = true;
            await _context.SaveChangesAsync();
        }

        /// <summary>Marca una notificación específica como leída.</summary>
        public async Task MarcarLeidaAsync(int idNotificacion)
        {
            var n = await _context.Notificaciones.FindAsync(idNotificacion);
            if (n != null) { n.Leida = true; await _context.SaveChangesAsync(); }
        }
    }
}
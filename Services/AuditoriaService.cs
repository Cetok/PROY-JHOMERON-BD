using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using Microsoft.AspNetCore.Http;

namespace PROYJHOME2026.Services
{
    public class AuditoriaService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Registra una acción de auditoría.
        /// Accion: "Crear" | "Editar" | "Eliminar" | "CambioEstado" | "Login" | "Logout"
        /// </summary>
        public async Task RegistrarAsync(
            string accion,
            string entidad,
            int? idEntidad,
            string descripcion,
            string? datosAnteriores = null)
        {
            var ctx = _httpContextAccessor.HttpContext;

            int? idUsuario = null;
            string? nombreUsuario = null;

            if (ctx?.Session != null)
            {
                var idStr = ctx.Session.GetString("UsuarioId");
                if (int.TryParse(idStr, out int id)) idUsuario = id;
                nombreUsuario = ctx.Session.GetString("UsuarioNombre");
            }

            string? ip = ctx?.Connection?.RemoteIpAddress?.ToString();

            var log = new AuditoriaLog
            {
                IdUsuario       = idUsuario,
                NombreUsuario   = nombreUsuario,
                Accion          = accion,
                Entidad         = entidad,
                IdEntidad       = idEntidad,
                Descripcion     = descripcion,
                DatosAnteriores = datosAnteriores,
                FechaHora       = DateTime.Now,
                IpCliente       = ip
            };

            _context.AuditoriaLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
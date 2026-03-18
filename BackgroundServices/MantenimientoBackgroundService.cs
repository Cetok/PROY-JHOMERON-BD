using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.BackgroundServices
{
    /// <summary>
    /// Servicio que corre en segundo plano cada hora.
    /// Revisa mantenimientos con fecha programada = hoy y estado Pendiente,
    /// crea notificaciones y envía emails a los usuarios.
    /// </summary>
    public class MantenimientoBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MantenimientoBackgroundService> _logger;

        // Revisar cada hora
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(1);

        public MantenimientoBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MantenimientoBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MantenimientoBackgroundService iniciado.");

            // Ejecutar inmediatamente al arrancar, luego cada hora
            while (!stoppingToken.IsCancellationRequested)
            {
                await RevisarMantenimientosPendientesAsync();
                await Task.Delay(_intervalo, stoppingToken);
            }
        }

        private async Task RevisarMantenimientosPendientesAsync()
        {
            try
            {
                using var scope   = _scopeFactory.CreateScope();
                var context       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notifService  = scope.ServiceProvider.GetRequiredService<NotificacionService>();
                var emailService  = scope.ServiceProvider.GetRequiredService<EmailService>();

                var hoy   = DateTime.Today;
                var manana = hoy.AddDays(1);

                // Mantenimientos pendientes cuya fecha programada es HOY o MAÑANA
                var pendientes = await context.MantenimientosCarros
                    .Include(m => m.Carro)
                    .Include(m => m.TipoMantenimiento)
                    .Include(m => m.UsuarioCreador)
                    .Where(m => m.Estado == "Pendiente"
                             && m.FechaProgramada.Date >= hoy
                             && m.FechaProgramada.Date <= manana)
                    .ToListAsync();

                var usuarios = await context.Usuarios
                    .Where(u => u.activo && u.correo != null)
                    .ToListAsync();

                foreach (var m in pendientes)
                {
                    var esHoy   = m.FechaProgramada.Date == hoy;
                    var esManana = m.FechaProgramada.Date == manana;

                    var titulo  = esHoy
                        ? $"⚙️ Mantenimiento HOY — {m.Carro?.Placa}"
                        : $"📅 Mantenimiento mañana — {m.Carro?.Placa}";

                    var mensaje = esHoy
                        ? $"El mantenimiento de {m.TipoMantenimiento?.Nombre} para {m.Carro?.Placa} está programado para hoy. Ingresa y dale a Proceder."
                        : $"Recuerda: mañana hay mantenimiento de {m.TipoMantenimiento?.Nombre} para {m.Carro?.Placa}.";

                    // Verificar que no exista ya una notificación de hoy para este mantenimiento
                    bool yaNotificado = await context.Notificaciones
                        .AnyAsync(n => n.IdMante == m.IdMante
                                    && n.FechaCreacion.Date == hoy
                                    && n.Titulo == titulo);

                    if (!yaNotificado)
                    {
                        // Crear notificación para todos los usuarios activos
                        await notifService.CrearAsync(
                            tipo:    "Mantenimiento",
                            titulo:  titulo,
                            mensaje: mensaje,
                            url:     $"/MantenimientoCarros/Details/{m.IdMante}",
                            idMante: m.IdMante
                        );

                        // Enviar email a cada usuario activo con correo
                        if (esHoy)
                        {
                            foreach (var usuario in usuarios)
                            {
                                if (!string.IsNullOrEmpty(usuario.correo))
                                {
                                    await emailService.EnviarAlertaMantenimientoAsync(
                                        destinatario:       usuario.correo,
                                        nombreUsuario:      usuario.nombreCompleto ?? usuario.username,
                                        placa:              m.Carro?.Placa ?? "—",
                                        tipoMantenimiento:  m.TipoMantenimiento?.Nombre ?? "—",
                                        fechaProgramada:    m.FechaProgramada,
                                        idMante:            m.IdMante
                                    );
                                }
                            }
                        }

                        _logger.LogInformation(
                            "Notificación creada para mantenimiento #{id} — {placa} ({fecha})",
                            m.IdMante, m.Carro?.Placa, m.FechaProgramada.ToString("dd/MM/yyyy"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en MantenimientoBackgroundService");
            }
        }
    }
}
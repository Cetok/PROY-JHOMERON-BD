using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.BackgroundServices
{
    public class MantenimientoBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MantenimientoBackgroundService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(1);

        // Horas en que se envían las alertas de modalidad/seguro (mañana, tarde, noche)
        private static readonly int[] HorasAlerta = { 8, 13, 20 };

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
            while (!stoppingToken.IsCancellationRequested)
            {
                await RevisarMantenimientosPendientesAsync();
                await RevisarModalidadesYSegurosAsync();
                await RevisarHabilitacionesVehicularesAsync();
                await Task.Delay(_intervalo, stoppingToken);
            }
        }

        // ── Mantenimientos ────────────────────────────────────────
        private async Task RevisarMantenimientosPendientesAsync()
        {
            try
            {
                using var scope  = _scopeFactory.CreateScope();
                var context      = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notifService = scope.ServiceProvider.GetRequiredService<NotificacionService>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                var hoy    = DateTime.Today;
                var manana = hoy.AddDays(1);

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
                    var esHoy    = m.FechaProgramada.Date == hoy;
                    var esManana = m.FechaProgramada.Date == manana;

                    var titulo  = esHoy
                        ? $"⚙️ Mantenimiento HOY — {m.Carro?.Placa}"
                        : $"📅 Mantenimiento mañana — {m.Carro?.Placa}";
                    var mensaje = esHoy
                        ? $"El mantenimiento de {m.TipoMantenimiento?.Nombre} para {m.Carro?.Placa} está programado para hoy."
                        : $"Recuerda: mañana hay mantenimiento de {m.TipoMantenimiento?.Nombre} para {m.Carro?.Placa}.";

                    bool yaNotificado = await context.Notificaciones
                        .AnyAsync(n => n.IdMante == m.IdMante
                                    && n.FechaCreacion.Date == hoy
                                    && n.Titulo == titulo);

                    if (!yaNotificado)
                    {
                        await notifService.CrearAsync("Mantenimiento", titulo, mensaje,
                            $"/MantenimientoCarros/Details/{m.IdMante}", m.IdMante);

                        if (esHoy)
                        {
                            foreach (var u in usuarios.Where(u => !string.IsNullOrEmpty(u.correo)))
                            {
                                await emailService.EnviarAlertaMantenimientoAsync(
                                    u.correo!, u.nombreCompleto ?? u.username,
                                    m.Carro?.Placa ?? "—", m.TipoMantenimiento?.Nombre ?? "—",
                                    m.FechaProgramada, m.IdMante);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando mantenimientos"); }
        }

        // ── Modalidades y Seguros — 3 veces al día ───────────────
        private async Task RevisarModalidadesYSegurosAsync()
        {
            var horaActual = DateTime.Now.Hour;
            if (!HorasAlerta.Contains(horaActual)) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context     = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var hoy          = DateTime.Today;
                var claveHoy     = hoy.ToString("yyyyMMdd");
                var umbralProximo = hoy.AddDays(30); // alertar si vence en 30 días

                // Obtener IDs de usuarios Admin y Transporte
                var destinatarios = await context.Usuarios
                    .Where(u => u.activo && (u.rol == "Admin" || u.rol == "Transporte"))
                    .ToListAsync();

                // ── MODALIDADES ──────────────────────────────────
                var modalidades = await context.CarroModalidades
                    .Include(cm => cm.Carro)
                    .Include(cm => cm.Modalidad)
                    .Where(cm => cm.FechaVencimiento.HasValue)
                    .ToListAsync();

                foreach (var cm in modalidades)
                {
                    var vence    = cm.FechaVencimiento!.Value.Date;
                    var diasRest = (vence - hoy).Days;
                    string titulo, mensaje, tipo;

                    if (diasRest < 0)
                    {
                        titulo  = $"🔴 Modalidad VENCIDA — {cm.Carro?.Placa}";
                        mensaje = $"La modalidad '{cm.Modalidad?.TipoModalidad}' del vehículo {cm.Carro?.Placa} venció el {vence:dd/MM/yyyy}. Renuévala a la brevedad.";
                        tipo    = "CambioEstado";
                    }
                    else if (diasRest <= 30)
                    {
                        titulo  = $"⚠️ Modalidad por vencer — {cm.Carro?.Placa}";
                        mensaje = $"La modalidad '{cm.Modalidad?.TipoModalidad}' del vehículo {cm.Carro?.Placa} vence en {diasRest} día(s) ({vence:dd/MM/yyyy}).";
                        tipo    = "Mantenimiento";
                    }
                    else continue;

                    // Verificar que no se haya notificado en esta hora de hoy
                    var claveTitulo = $"{titulo}|{claveHoy}|H{horaActual}";
                    bool yaNotif = await context.Notificaciones
                        .AnyAsync(n => n.Titulo == titulo
                                    && n.FechaCreacion.Date == hoy
                                    && n.FechaCreacion.Hour == horaActual);
                    if (yaNotif) continue;

                    foreach (var u in destinatarios)
                    {
                        context.Notificaciones.Add(new Notificacion
                        {
                            IdUsuario     = u.idUsuario,
                            Tipo          = tipo,
                            Titulo        = titulo,
                            Mensaje       = mensaje,
                            Url           = $"/Carros/Details/{cm.IdCarro}",
                            Leida         = false,
                            FechaCreacion = DateTime.Now
                        });
                    }
                }

                // ── SEGUROS ──────────────────────────────────────
                var seguros = await context.CarroSeguros
                    .Include(cs => cs.Carro)
                    .Include(cs => cs.Seguro)
                    .Where(cs => cs.FechaCulminada.HasValue)
                    .ToListAsync();

                foreach (var cs in seguros)
                {
                    var vence    = cs.FechaCulminada!.Value.Date;
                    var diasRest = (vence - hoy).Days;
                    string titulo, mensaje, tipo;

                    if (diasRest < 0)
                    {
                        titulo  = $"🔴 Seguro VENCIDO — {cs.Carro?.Placa}";
                        mensaje = $"El seguro '{cs.Seguro?.TipoSeguro}' del vehículo {cs.Carro?.Placa} venció el {vence:dd/MM/yyyy}. Renuévalo a la brevedad.";
                        tipo    = "CambioEstado";
                    }
                    else if (diasRest <= 30)
                    {
                        titulo  = $"⚠️ Seguro por vencer — {cs.Carro?.Placa}";
                        mensaje = $"El seguro '{cs.Seguro?.TipoSeguro}' del vehículo {cs.Carro?.Placa} vence en {diasRest} día(s) ({vence:dd/MM/yyyy}).";
                        tipo    = "Mantenimiento";
                    }
                    else continue;

                    bool yaNotif = await context.Notificaciones
                        .AnyAsync(n => n.Titulo == titulo
                                    && n.FechaCreacion.Date == hoy
                                    && n.FechaCreacion.Hour == horaActual);
                    if (yaNotif) continue;

                    foreach (var u in destinatarios)
                    {
                        context.Notificaciones.Add(new Notificacion
                        {
                            IdUsuario     = u.idUsuario,
                            Tipo          = tipo,
                            Titulo        = titulo,
                            Mensaje       = mensaje,
                            Url           = $"/Carros/Details/{cs.IdCarro}",
                            Leida         = false,
                            FechaCreacion = DateTime.Now
                        });
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Revisión modalidades/seguros completada — hora {h}:00", horaActual);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando modalidades/seguros"); }
        }

        // ── Habilitaciones Vehiculares — 3 veces al día ──────────
        private async Task RevisarHabilitacionesVehicularesAsync()
        {
            var horaActual = DateTime.Now.Hour;
            if (!HorasAlerta.Contains(horaActual)) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context     = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var hoy = DateTime.Today;

                var destinatarios = await context.Usuarios
                    .Where(u => u.activo && (u.rol == "Admin" || u.rol == "Transporte"))
                    .ToListAsync();

                var habilitaciones = await context.HabilitacionesVehiculares
                    .Include(h => h.Carro)
                    .Where(h => h.EsVigente)
                    .ToListAsync();

                foreach (var h in habilitaciones)
                {
                    var vence    = h.FechaCulminacion.Date;
                    var diasRest = (vence - hoy).Days;
                    string titulo, mensaje, tipo;

                    if (diasRest < 0)
                    {
                        titulo  = $"🔴 Hab. Vehicular VENCIDA — {h.Carro?.Placa}";
                        mensaje = $"La habilitación vehicular [{h.Codigo}] del vehículo {h.Carro?.Placa} venció el {vence:dd/MM/yyyy}. Renuévala a la brevedad.";
                        tipo    = "CambioEstado";
                    }
                    else if (diasRest <= 30)
                    {
                        titulo  = $"⚠️ Hab. Vehicular por vencer — {h.Carro?.Placa}";
                        mensaje = $"La habilitación vehicular [{h.Codigo}] del vehículo {h.Carro?.Placa} vence en {diasRest} día(s) ({vence:dd/MM/yyyy}).";
                        tipo    = "Mantenimiento";
                    }
                    else continue;

                    bool yaNotif = await context.Notificaciones
                        .AnyAsync(n => n.Titulo == titulo
                                    && n.FechaCreacion.Date == hoy
                                    && n.FechaCreacion.Hour == horaActual);
                    if (yaNotif) continue;

                    foreach (var u in destinatarios)
                    {
                        context.Notificaciones.Add(new Notificacion
                        {
                            IdUsuario     = u.idUsuario,
                            Tipo          = tipo,
                            Titulo        = titulo,
                            Mensaje       = mensaje,
                            Url           = $"/Carros/Details/{h.IdCarro}",
                            Leida         = false,
                            FechaCreacion = DateTime.Now
                        });
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Revisión habilitaciones vehiculares completada — hora {h}:00", horaActual);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando habilitaciones vehiculares"); }
        }
    }
}
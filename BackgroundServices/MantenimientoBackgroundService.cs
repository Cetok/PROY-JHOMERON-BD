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

        // Horas en que se envían las alertas (mañana, tarde, noche)
        private static readonly int[] HorasAlerta = { 8, 13, 20 };

        // Días de anticipación para alertas WhatsApp
        private const int DiasUmbralWsp = 30;

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

        // ══════════════════════════════════════════════════════════
        // MANTENIMIENTOS — notifica hoy/mañana + WhatsApp ≤10 días
        // ══════════════════════════════════════════════════════════
        private async Task RevisarMantenimientosPendientesAsync()
        {
            try
            {
                using var scope  = _scopeFactory.CreateScope();
                var context      = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notifService = scope.ServiceProvider.GetRequiredService<NotificacionService>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
                var twilioService   = scope.ServiceProvider.GetRequiredService<TwilioService>();

                var hoy    = DateTime.Today;
                var manana = hoy.AddDays(1);

                // ── Notificación interna: hoy o mañana ──────────
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

                // ── WhatsApp: mantenimientos en los próximos 10 días (solo a las 8h, 13h, 20h) ──
                var horaActualMante = DateTime.Now.Hour;
                if (HorasAlerta.Contains(horaActualMante))
                {
                var proximos10 = await context.MantenimientosCarros
                    .Include(m => m.Carro)
                    .Include(m => m.TipoMantenimiento)
                    .Where(m => m.Estado == "Pendiente"
                             && m.FechaProgramada.Date >= hoy
                             && m.FechaProgramada.Date <= hoy.AddDays(DiasUmbralWsp))
                    .ToListAsync();

                foreach (var m in proximos10)
                {
                    var dias        = (m.FechaProgramada.Date - hoy).Days;
                    var claveMsgWsp = $"mante_{m.IdMante}_{dias}dias_h{horaActualMante}";

                    string txtWsp;
                    if (dias == 0)
                        txtWsp = $"🔧 *MANTENIMIENTO HOY*\n" +
                                 $"Vehículo: {m.Carro?.Placa}\n" +
                                 $"Tipo: {m.TipoMantenimiento?.Nombre}\n" +
                                 $"Fecha programada: {m.FechaProgramada:dd/MM/yyyy}";
                    else
                        txtWsp = $"🔧 *Mantenimiento en {dias} día(s)*\n" +
                                 $"Vehículo: {m.Carro?.Placa}\n" +
                                 $"Tipo: {m.TipoMantenimiento?.Nombre}\n" +
                                 $"Fecha programada: {m.FechaProgramada:dd/MM/yyyy}";

                    await twilioService.EnviarATodosAsync(claveMsgWsp, txtWsp);
                }
                } // fin if HorasAlerta
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando mantenimientos"); }
        }

        // ══════════════════════════════════════════════════════════
        // MODALIDADES Y SEGUROS — 3 veces al día + WhatsApp ≤10 días
        // ══════════════════════════════════════════════════════════
        private async Task RevisarModalidadesYSegurosAsync()
        {
            var horaActual = DateTime.Now.Hour;
            if (!HorasAlerta.Contains(horaActual)) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var twilioService  = scope.ServiceProvider.GetRequiredService<TwilioService>();

                var hoy = DateTime.Today;

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

                    bool yaNotif = await context.Notificaciones
                        .AnyAsync(n => n.Titulo == titulo
                                    && n.FechaCreacion.Date == hoy
                                    && n.FechaCreacion.Hour == horaActual);
                    if (!yaNotif)
                    {
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

                    // WhatsApp si vence en 10 días o menos (incluyendo vencidos)
                    if (diasRest <= DiasUmbralWsp)
                    {
                        string txtWsp;
                        if (diasRest < 0)
                            txtWsp = $"🔴 *Modalidad VENCIDA*\n" +
                                     $"Vehículo: {cm.Carro?.Placa}\n" +
                                     $"Tipo: {cm.Modalidad?.TipoModalidad}\n" +
                                     $"Venció el: {vence:dd/MM/yyyy}\n" +
                                     $"Por favor renovar a la brevedad.";
                        else if (diasRest == 0)
                            txtWsp = $"🚨 *Modalidad vence HOY*\n" +
                                     $"Vehículo: {cm.Carro?.Placa}\n" +
                                     $"Tipo: {cm.Modalidad?.TipoModalidad}\n" +
                                     $"Fecha vencimiento: {vence:dd/MM/yyyy}";
                        else
                            txtWsp = $"⚠️ *Modalidad por vencer en {diasRest} día(s)*\n" +
                                     $"Vehículo: {cm.Carro?.Placa}\n" +
                                     $"Tipo: {cm.Modalidad?.TipoModalidad}\n" +
                                     $"Fecha vencimiento: {vence:dd/MM/yyyy}";

                        var clave = $"modalidad_{cm.IdCarro}_{cm.IdModalidad}_{diasRest}dias_h{horaActual}";
                        await twilioService.EnviarATodosAsync(clave, txtWsp);
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
                    if (!yaNotif)
                    {
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

                    // WhatsApp ≤10 días
                    if (diasRest <= DiasUmbralWsp)
                    {
                        string txtWsp;
                        if (diasRest < 0)
                            txtWsp = $"🔴 *Seguro VENCIDO*\n" +
                                     $"Vehículo: {cs.Carro?.Placa}\n" +
                                     $"Tipo: {cs.Seguro?.TipoSeguro}\n" +
                                     $"Venció el: {vence:dd/MM/yyyy}\n" +
                                     $"Por favor renovar a la brevedad.";
                        else if (diasRest == 0)
                            txtWsp = $"🚨 *Seguro vence HOY*\n" +
                                     $"Vehículo: {cs.Carro?.Placa}\n" +
                                     $"Tipo: {cs.Seguro?.TipoSeguro}\n" +
                                     $"Fecha vencimiento: {vence:dd/MM/yyyy}";
                        else
                            txtWsp = $"⚠️ *Seguro por vencer en {diasRest} día(s)*\n" +
                                     $"Vehículo: {cs.Carro?.Placa}\n" +
                                     $"Tipo: {cs.Seguro?.TipoSeguro}\n" +
                                     $"Fecha vencimiento: {vence:dd/MM/yyyy}";

                        var clave = $"seguro_{cs.IdCarro}_{cs.IdSeguro}_{diasRest}dias_h{horaActual}";
                        await twilioService.EnviarATodosAsync(clave, txtWsp);
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Revisión modalidades/seguros completada — hora {h}:00", horaActual);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando modalidades/seguros"); }
        }

        // ══════════════════════════════════════════════════════════
        // HABILITACIONES VEHICULARES — 3 veces al día + WhatsApp ≤10 días
        // ══════════════════════════════════════════════════════════
        private async Task RevisarHabilitacionesVehicularesAsync()
        {
            var horaActual = DateTime.Now.Hour;
            if (!HorasAlerta.Contains(horaActual)) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var twilioService  = scope.ServiceProvider.GetRequiredService<TwilioService>();

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
                    if (!yaNotif)
                    {
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

                    // WhatsApp ≤10 días
                    if (diasRest <= DiasUmbralWsp)
                    {
                        string txtWsp;
                        if (diasRest < 0)
                            txtWsp = $"🔴 *Revisión Técnica VENCIDA*\n" +
                                     $"Vehículo: {h.Carro?.Placa}\n" +
                                     $"Código: {h.Codigo}\n" +
                                     $"Venció el: {vence:dd/MM/yyyy}\n" +
                                     $"Por favor renovar a la brevedad.";
                        else if (diasRest == 0)
                            txtWsp = $"🚨 *Revisión Técnica vence HOY*\n" +
                                     $"Vehículo: {h.Carro?.Placa}\n" +
                                     $"Código: {h.Codigo}\n" +
                                     $"Fecha vencimiento: {vence:dd/MM/yyyy}";
                        else
                            txtWsp = $"⚠️ *Revisión Técnica por vencer en {diasRest} día(s)*\n" +
                                     $"Vehículo: {h.Carro?.Placa}\n" +
                                     $"Código: {h.Codigo}\n" +
                                     $"Fecha vencimiento: {vence:dd/MM/yyyy}";

                        var clave = $"habveh_{h.IdHabilitacion}_{diasRest}dias_h{horaActual}";
                        await twilioService.EnviarATodosAsync(clave, txtWsp);
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Revisión habilitaciones vehiculares completada — hora {h}:00", horaActual);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando habilitaciones vehiculares"); }
        }
    }
}
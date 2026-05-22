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
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(15);

        // 3 alertas el día del vencimiento
        private static readonly int[] HorasVencimientoHoy = { 8, 11, 15 };
        // 1 alerta diaria cuando quedan 7 días (a las 8am)
        private const int HoraAlertaSemanal = 8;
        // Días de anticipación para alertar
        private const int DiasUmbral = 7;

        public MantenimientoBackgroundService(IServiceScopeFactory scopeFactory,
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
                try
                {
                    await RevisarMantenimientosPendientesAsync();
                    await RevisarVencimientosCarroAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error general en BackgroundService");
                }
                await Task.Delay(_intervalo, stoppingToken);
            }
        }

        // Devuelve true si la hora actual está dentro de los primeros 14 minutos de la hora dada
        private static bool EnVentana(int hora) =>
            DateTime.Now.Hour == hora && DateTime.Now.Minute < 15;

        // Clave única para evitar duplicados en el día + hora
        private static string Clave(string tipo, object id, int diasRest, int? hora = null) =>
            hora.HasValue
                ? $"{tipo}_{id}_{DateTime.Today:yyyyMMdd}_h{hora}"
                : $"{tipo}_{id}_{DateTime.Today:yyyyMMdd}";

        // ══════════════════════════════════════════════════════════
        // MANTENIMIENTOS DE CARROS
        // ══════════════════════════════════════════════════════════
        private async Task RevisarMantenimientosPendientesAsync()
        {
            try
            {
                using var scope     = _scopeFactory.CreateScope();
                var context         = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notifService    = scope.ServiceProvider.GetRequiredService<NotificacionService>();
                var emailService    = scope.ServiceProvider.GetRequiredService<EmailService>();
                var twilioService   = scope.ServiceProvider.GetRequiredService<TwilioService>();

                var hoy    = DateTime.Today;
                var manana = hoy.AddDays(1);

                // Usuarios Admin y Transporte con correo
                var usuarios = await context.Usuarios
                    .Where(u => u.activo && u.correo != null &&
                                (u.rol == "Admin" || u.rol == "Transporte"))
                    .ToListAsync();

                // Notificación interna: hoy o mañana
                var pendientes = await context.MantenimientosCarros
                    .Include(m => m.Carro)
                    .Include(m => m.TipoMantenimiento)
                    .Where(m => m.Estado == "Pendiente"
                             && m.FechaProgramada.Date >= hoy
                             && m.FechaProgramada.Date <= manana)
                    .ToListAsync();

                foreach (var m in pendientes)
                {
                    var esHoy  = m.FechaProgramada.Date == hoy;
                    var titulo = esHoy
                        ? $"⚙️ Mantenimiento HOY — {m.Carro?.Placa}"
                        : $"📅 Mantenimiento mañana — {m.Carro?.Placa}";

                    bool yaNotif = await context.Notificaciones
                        .AnyAsync(n => n.IdMante == m.IdMante
                                    && n.FechaCreacion.Date == hoy
                                    && n.Titulo == titulo);
                    if (!yaNotif)
                    {
                        await notifService.CrearAsync("Mantenimiento", titulo,
                            $"Mantenimiento de {m.TipoMantenimiento?.Nombre} para {m.Carro?.Placa} — {m.FechaProgramada:dd/MM/yyyy}",
                            $"/MantenimientoCarros/Details/{m.IdMante}", m.IdMante);
                    }

                    // Email: solo si es hoy, en ventanas 8h, 11h, 15h
                    if (esHoy)
                    {
                        foreach (var hora in HorasVencimientoHoy)
                        {
                            if (!EnVentana(hora)) continue;
                            var claveEmail = Clave("mante_email", m.IdMante, 0, hora);
                            bool yaEmail = await context.Notificaciones
                                .AnyAsync(n => n.Titulo == $"EMAIL_SENT_{claveEmail}");
                            if (!yaEmail)
                            {
                                foreach (var u in usuarios)
                                    await emailService.EnviarAlertaMantenimientoAsync(
                                        u.correo!, u.nombreCompleto ?? u.username,
                                        m.Carro?.Placa ?? "—", m.TipoMantenimiento?.Nombre ?? "—",
                                        m.FechaProgramada, m.IdMante);

                                // WhatsApp
                                await twilioService.EnviarATodosAsync(
                                    Clave("mante_wsp", m.IdMante, 0, hora),
                                    $"🔧 *Mantenimiento HOY*\nVehículo: {m.Carro?.Placa}\nTipo: {m.TipoMantenimiento?.Nombre}\nFecha: {m.FechaProgramada:dd/MM/yyyy}");

                                // Marcar como enviado
                                context.Notificaciones.Add(new Notificacion
                                {
                                    IdUsuario     = usuarios.FirstOrDefault()?.idUsuario ?? 0,
                                    Tipo          = "Sistema",
                                    Titulo        = $"EMAIL_SENT_{claveEmail}",
                                    Leida         = true,
                                    FechaCreacion = DateTime.Now
                                });
                                await context.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando mantenimientos"); }
        }

        // ══════════════════════════════════════════════════════════
        // VENCIMIENTOS: MODALIDADES, SEGUROS, HABILITACIONES
        // ══════════════════════════════════════════════════════════
        private async Task RevisarVencimientosCarroAsync()
        {
            try
            {
                using var scope   = _scopeFactory.CreateScope();
                var context       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService  = scope.ServiceProvider.GetRequiredService<EmailService>();
                var twilioService = scope.ServiceProvider.GetRequiredService<TwilioService>();

                var hoy      = DateTime.Today;
                var horaActual = DateTime.Now.Hour;

                var usuarios = await context.Usuarios
                    .Where(u => u.activo && u.correo != null &&
                                (u.rol == "Admin" || u.rol == "Transporte"))
                    .ToListAsync();

                // Modalidades
                var modalidades = await context.CarroModalidades
                    .Include(cm => cm.Carro).Include(cm => cm.Modalidad)
                    .Where(cm => cm.FechaVencimiento.HasValue).ToListAsync();

                foreach (var cm in modalidades)
                    await ProcesarVencimiento(context, emailService, twilioService, usuarios,
                        hoy, horaActual,
                        fechaVenc:    cm.FechaVencimiento!.Value.Date,
                        tipoDoc:      $"Modalidad {cm.Modalidad?.TipoModalidad}",
                        placa:        cm.Carro?.Placa ?? "—",
                        claveBase:    $"modal_{cm.IdCarro}_{cm.IdModalidad}",
                        urlDetalle:   $"/Carros/Details/{cm.IdCarro}");

                // Seguros
                var seguros = await context.CarroSeguros
                    .Include(cs => cs.Carro).Include(cs => cs.Seguro)
                    .Where(cs => cs.FechaCulminada.HasValue).ToListAsync();

                foreach (var cs in seguros)
                    await ProcesarVencimiento(context, emailService, twilioService, usuarios,
                        hoy, horaActual,
                        fechaVenc:    cs.FechaCulminada!.Value.Date,
                        tipoDoc:      $"Seguro {cs.Seguro?.TipoSeguro}",
                        placa:        cs.Carro?.Placa ?? "—",
                        claveBase:    $"seguro_{cs.IdCarro}_{cs.IdSeguro}",
                        urlDetalle:   $"/Carros/Details/{cs.IdCarro}");

                // Habilitaciones vehiculares
                var habilitaciones = await context.HabilitacionesVehiculares
                    .Include(h => h.Carro).Where(h => h.EsVigente).ToListAsync();

                foreach (var h in habilitaciones)
                    await ProcesarVencimiento(context, emailService, twilioService, usuarios,
                        hoy, horaActual,
                        fechaVenc:    h.FechaCulminacion.Date,
                        tipoDoc:      $"Habilitación Vehicular [{h.Codigo}]",
                        placa:        h.Carro?.Placa ?? "—",
                        claveBase:    $"habveh_{h.IdHabilitacion}",
                        urlDetalle:   $"/Carros/Details/{h.IdCarro}");

                await context.SaveChangesAsync();
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando vencimientos de carro"); }
        }

        // ── Procesar un vencimiento individual ───────────────────
        private async Task ProcesarVencimiento(
            AppDbContext context, EmailService emailService,
            TwilioService twilioService, List<Usuario> usuarios,
            DateTime hoy, int horaActual,
            DateTime fechaVenc, string tipoDoc, string placa,
            string claveBase, string urlDetalle)
        {
            var diasRest = (fechaVenc - hoy).Days;

            // Solo procesar si está vencido, vence hoy, o quedan exactamente 7 días
            bool esHoy    = diasRest == 0;
            bool es7dias  = diasRest == DiasUmbral;
            bool vencido  = diasRest < 0;

            if (!esHoy && !es7dias && !vencido) return;

            // ── Caso 1: Vence hoy o ya venció → 3 veces al día ──
            if (esHoy || vencido)
            {
                foreach (var hora in HorasVencimientoHoy)
                {
                    if (!EnVentana(hora)) continue;

                    var claveEnvio = $"EMAIL_SENT_{claveBase}_{hoy:yyyyMMdd}_h{hora}";
                    bool yaEnviado = await context.Notificaciones
                        .AnyAsync(n => n.Titulo == claveEnvio);
                    if (yaEnviado) continue;

                    // Notificación interna
                    foreach (var u in usuarios)
                    {
                        var titulo  = vencido
                            ? $"🔴 {tipoDoc} VENCIDO — {placa}"
                            : $"🚨 {tipoDoc} vence HOY — {placa}";
                        var mensaje = vencido
                            ? $"{tipoDoc} del vehículo {placa} venció el {fechaVenc:dd/MM/yyyy}. Renuévalo a la brevedad."
                            : $"{tipoDoc} del vehículo {placa} vence HOY {fechaVenc:dd/MM/yyyy}.";
                        context.Notificaciones.Add(new Notificacion
                        {
                            IdUsuario     = u.idUsuario,
                            Tipo          = "CambioEstado",
                            Titulo        = titulo,
                            Mensaje       = mensaje,
                            Url           = urlDetalle,
                            Leida         = false,
                            FechaCreacion = DateTime.Now
                        });
                    }

                    // Email
                    foreach (var u in usuarios)
                        await emailService.EnviarAlertaVencimientoAsync(
                            u.correo!, u.nombreCompleto ?? u.username,
                            tipoDoc, placa, fechaVenc, diasRest);

                    // WhatsApp
                    var txtWsp = vencido
                        ? $"🔴 *{tipoDoc} VENCIDO*\nVehículo: {placa}\nVenció el: {fechaVenc:dd/MM/yyyy}\nRenovar a la brevedad."
                        : $"🚨 *{tipoDoc} vence HOY*\nVehículo: {placa}\nFecha: {fechaVenc:dd/MM/yyyy}";
                    await twilioService.EnviarATodosAsync($"{claveBase}_{hoy:yyyyMMdd}_h{hora}", txtWsp);

                    // Marcar enviado
                    context.Notificaciones.Add(new Notificacion
                    {
                        IdUsuario = usuarios.FirstOrDefault()?.idUsuario ?? 0,
                        Tipo      = "Sistema", Titulo = claveEnvio,
                        Leida = true, FechaCreacion = DateTime.Now
                    });
                    await context.SaveChangesAsync();
                    break; // Solo una ventana por revisión
                }
                return;
            }

            // ── Caso 2: Quedan exactamente 7 días → 1 vez al día a las 8am ──
            if (es7dias && EnVentana(HoraAlertaSemanal))
            {
                var claveEnvio = $"EMAIL_SENT_{claveBase}_{hoy:yyyyMMdd}_7dias";
                bool yaEnviado = await context.Notificaciones
                    .AnyAsync(n => n.Titulo == claveEnvio);
                if (yaEnviado) return;

                var titulo7  = $"⚠️ {tipoDoc} vence en 7 días — {placa}";
                var mensaje7 = $"{tipoDoc} del vehículo {placa} vence en 7 días ({fechaVenc:dd/MM/yyyy}). Gestiona la renovación.";

                // Notificación interna
                foreach (var u in usuarios)
                    context.Notificaciones.Add(new Notificacion
                    {
                        IdUsuario     = u.idUsuario,
                        Tipo          = "Mantenimiento",
                        Titulo        = titulo7,
                        Mensaje       = mensaje7,
                        Url           = urlDetalle,
                        Leida         = false,
                        FechaCreacion = DateTime.Now
                    });

                // Email
                foreach (var u in usuarios)
                    await emailService.EnviarAlertaVencimientoAsync(
                        u.correo!, u.nombreCompleto ?? u.username,
                        tipoDoc, placa, fechaVenc, diasRest);

                // WhatsApp
                await twilioService.EnviarATodosAsync(
                    $"{claveBase}_{hoy:yyyyMMdd}_7dias",
                    $"⚠️ *{tipoDoc} vence en 7 días*\nVehículo: {placa}\nFecha vencimiento: {fechaVenc:dd/MM/yyyy}\nGestiona la renovación.");

                // Marcar enviado
                context.Notificaciones.Add(new Notificacion
                {
                    IdUsuario = usuarios.FirstOrDefault()?.idUsuario ?? 0,
                    Tipo = "Sistema", Titulo = claveEnvio,
                    Leida = true, FechaCreacion = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
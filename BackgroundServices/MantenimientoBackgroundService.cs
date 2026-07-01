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

        // Solo notificar a las 9am (ventana de 14 minutos)
        private const int HoraNotificacion = 9;

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
                    // Solo ejecutar lógica de alertas en la ventana de las 9am
                    if (EnVentana(HoraNotificacion))
                    {
                        await RevisarMantenimientosPendientesAsync();
                        await RevisarVencimientosCarroAsync();
                    }
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

        // ══════════════════════════════════════════════════════════
        // MANTENIMIENTOS DE CARROS (pendientes hoy o mañana)
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

                var usuarios = await context.Usuarios
                    .Where(u => u.activo && u.correo != null &&
                                (u.rol == "Admin" || u.rol == "Transporte"))
                    .ToListAsync();

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
                    var sufijo = esHoy ? "HOY" : "mañana";
                    var titulo = esHoy
                        ? $"⚙️ Mantenimiento HOY — {m.Carro?.Placa}"
                        : $"📅 Mantenimiento mañana — {m.Carro?.Placa}";
                    var clave  = $"EMAIL_SENT_mante_{m.IdMante}_{hoy:yyyyMMdd}_{(esHoy?"hoy":"manana")}";

                    bool yaEnviado = await context.Notificaciones
                        .AnyAsync(n => n.Titulo == clave);
                    if (yaEnviado) continue;

                    // Notificación interna (1 sola)
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

                    // Email (1 solo por día)
                    foreach (var u in usuarios)
                        await emailService.EnviarAlertaMantenimientoAsync(
                            u.correo!, u.nombreCompleto ?? u.username,
                            m.Carro?.Placa ?? "—", m.TipoMantenimiento?.Nombre ?? "—",
                            m.FechaProgramada, m.IdMante);

                    // WhatsApp (1 solo)
                    await twilioService.EnviarATodosAsync(clave,
                        $"⚙️ *Mantenimiento {sufijo}*\nVehículo: {m.Carro?.Placa}\nTipo: {m.TipoMantenimiento?.Nombre}\nFecha: {m.FechaProgramada:dd/MM/yyyy}");

                    // Marcar como enviado
                    context.Notificaciones.Add(new Notificacion
                    {
                        IdUsuario     = usuarios.FirstOrDefault()?.idUsuario ?? 0,
                        Tipo          = "Sistema",
                        Titulo        = clave,
                        Leida         = true,
                        FechaCreacion = DateTime.Now
                    });
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando mantenimientos"); }
        }

        // ══════════════════════════════════════════════════════════
        // VENCIMIENTOS: SEGUROS, MODALIDADES (REV. TÉCNICA), HABILITACIONES
        // Solo notifica: 1 día antes a 9am + el día que vence a 9am
        // Nunca notifica si ya venció
        // ══════════════════════════════════════════════════════════
        private async Task RevisarVencimientosCarroAsync()
        {
            try
            {
                using var scope   = _scopeFactory.CreateScope();
                var context       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService  = scope.ServiceProvider.GetRequiredService<EmailService>();
                var twilioService = scope.ServiceProvider.GetRequiredService<TwilioService>();

                var hoy = DateTime.Today;

                var usuarios = await context.Usuarios
                    .Where(u => u.activo && u.correo != null &&
                                (u.rol == "Admin" || u.rol == "Transporte"))
                    .ToListAsync();

                // ── Seguros ──────────────────────────────────────
                var seguros = await context.CarroSeguros
                    .Include(cs => cs.Carro).Include(cs => cs.Seguro)
                    .Where(cs => cs.FechaCulminada.HasValue).ToListAsync();

                foreach (var cs in seguros)
                    await ProcesarVencimiento(context, emailService, twilioService, usuarios,
                        hoy,
                        fechaVenc:  cs.FechaCulminada!.Value.Date,
                        tipoDoc:    $"Seguro {cs.Seguro?.TipoSeguro}",
                        placa:      cs.Carro?.Placa ?? "—",
                        claveBase:  $"seguro_{cs.IdCarro}_{cs.IdSeguro}",
                        urlDetalle: $"/Carros/Details/{cs.IdCarro}");

                // ── Modalidades (Revisión Técnica, etc.) ─────────
                var modalidades = await context.CarroModalidades
                    .Include(cm => cm.Carro).Include(cm => cm.Modalidad)
                    .Where(cm => cm.FechaVencimiento.HasValue).ToListAsync();

                foreach (var cm in modalidades)
                    await ProcesarVencimiento(context, emailService, twilioService, usuarios,
                        hoy,
                        fechaVenc:  cm.FechaVencimiento!.Value.Date,
                        tipoDoc:    $"Modalidad {cm.Modalidad?.TipoModalidad}",
                        placa:      cm.Carro?.Placa ?? "—",
                        claveBase:  $"modal_{cm.IdCarro}_{cm.IdModalidad}",
                        urlDetalle: $"/Carros/Details/{cm.IdCarro}");

                // ── Habilitaciones vehiculares ────────────────────
                var habilitaciones = await context.HabilitacionesVehiculares
                    .Include(h => h.Carro).Where(h => h.EsVigente).ToListAsync();

                foreach (var h in habilitaciones)
                    await ProcesarVencimiento(context, emailService, twilioService, usuarios,
                        hoy,
                        fechaVenc:  h.FechaCulminacion.Date,
                        tipoDoc:    $"Habilitación Vehicular [{h.Codigo}]",
                        placa:      h.Carro?.Placa ?? "—",
                        claveBase:  $"habveh_{h.IdHabilitacion}",
                        urlDetalle: $"/Carros/Details/{h.IdCarro}");

                await context.SaveChangesAsync();
            }
            catch (Exception ex) { _logger.LogError(ex, "Error revisando vencimientos de carro"); }
        }

        // ── Procesar un vencimiento individual ───────────────────
        // Solo 2 notificaciones en toda la vida del documento:
        //   1) 1 día antes  → clave "...pre"
        //   2) día que vence → clave "...hoy"
        // Si ya venció: no notifica nada
        private async Task ProcesarVencimiento(
            AppDbContext context, EmailService emailService,
            TwilioService twilioService, List<Usuario> usuarios,
            DateTime hoy,
            DateTime fechaVenc, string tipoDoc, string placa,
            string claveBase, string urlDetalle)
        {
            var diasRest = (fechaVenc - hoy).Days;

            // Solo procesar si vence mañana (1 día antes) o vence hoy exacto
            // Si ya venció (diasRest < 0) → no notificar más
            if (diasRest != 1 && diasRest != 0) return;

            var sufijoClave = diasRest == 0 ? "hoy" : "pre";
            var claveEnvio  = $"EMAIL_SENT_{claveBase}_{hoy:yyyyMMdd}_{sufijoClave}";

            bool yaEnviado = await context.Notificaciones
                .AnyAsync(n => n.Titulo == claveEnvio);
            if (yaEnviado) return;

            // Títulos y mensajes según si es hoy o mañana
            string titulo, mensaje, txtWsp;
            if (diasRest == 0)
            {
                titulo  = $"🚨 {tipoDoc} vence HOY — {placa}";
                mensaje = $"{tipoDoc} del vehículo {placa} vence HOY {fechaVenc:dd/MM/yyyy}. Gestiona la renovación de inmediato.";
                txtWsp  = $"🚨 *{tipoDoc} vence HOY*\nVehículo: {placa}\nFecha: {fechaVenc:dd/MM/yyyy}\nGestiona la renovación hoy.";
            }
            else // diasRest == 1
            {
                titulo  = $"⚠️ {tipoDoc} vence mañana — {placa}";
                mensaje = $"{tipoDoc} del vehículo {placa} vence mañana {fechaVenc:dd/MM/yyyy}. Gestiona la renovación.";
                txtWsp  = $"⚠️ *{tipoDoc} vence mañana*\nVehículo: {placa}\nFecha vencimiento: {fechaVenc:dd/MM/yyyy}\nGestiona la renovación.";
            }

            // Notificaciones internas (una por usuario)
            foreach (var u in usuarios)
            {
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

            // Email (1 solo por usuario)
            foreach (var u in usuarios)
                await emailService.EnviarAlertaVencimientoAsync(
                    u.correo!, u.nombreCompleto ?? u.username,
                    tipoDoc, placa, fechaVenc, diasRest);

            // WhatsApp (1 solo)
            await twilioService.EnviarATodosAsync($"{claveBase}_{hoy:yyyyMMdd}_{sufijoClave}", txtWsp);

            // Marcar como enviado para no repetir
            context.Notificaciones.Add(new Notificacion
            {
                IdUsuario     = usuarios.FirstOrDefault()?.idUsuario ?? 0,
                Tipo          = "Sistema",
                Titulo        = claveEnvio,
                Leida         = true,
                FechaCreacion = DateTime.Now
            });

            await context.SaveChangesAsync();
        }
    }
}
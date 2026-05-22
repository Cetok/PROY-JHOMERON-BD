using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PROYJHOME2026.Services
{
    public class EmailService
    {
        private readonly IConfiguration        _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── ENVÍO BASE con MailKit (soporta puerto 465 SSL implícito) ──
        public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            try
            {
                var host     = _config["Email:Host"]     ?? "mail.jhomeron.com";
                var port     = int.Parse(_config["Email:Port"] ?? "465");
                var usuario  = _config["Email:Usuario"]  ?? "sistemas@jhomeron.com";
                var password = _config["Email:Password"] ?? "";

                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress("Sistema Jhomeron S.A", usuario));
                mensaje.To.Add(new MailboxAddress(destinatario, destinatario));
                mensaje.Subject = asunto;
                mensaje.Body    = new TextPart("html") { Text = cuerpoHtml };

                using var client = new SmtpClient();
                // Puerto 465 = SslOnConnect (SSL implícito)
                // Puerto 587 = StartTls
                var socketOpts = port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(host, port, socketOpts);
                await client.AuthenticateAsync(usuario, password);
                await client.SendAsync(mensaje);
                await client.DisconnectAsync(true);

                _logger.LogInformation("✉ Email enviado a {dest}: {asunto}", destinatario, asunto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✉ Error enviando email a {dest}", destinatario);
            }
        }

        // ── PLANTILLA BASE HTML ──────────────────────────────────
        private static string PlantillaBase(string colorEncabezado, string iconoEncabezado,
            string subtituloEncabezado, string nombreUsuario, string contenidoTarjeta, string piePagina = "")
        {
            return $@"<!DOCTYPE html>
<html lang='es'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,Helvetica,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f1f5f9;padding:32px 0;'>
    <tr><td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='max-width:600px;width:100%;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.10);'>
        <tr>
          <td style='background:{colorEncabezado};padding:28px 32px;'>
            <table width='100%' cellpadding='0' cellspacing='0'>
              <tr>
                <td>
                  <div style='font-size:28px;margin-bottom:2px;'>{iconoEncabezado}</div>
                  <div style='color:white;font-size:20px;font-weight:700;'>Sistema Jhomeron S.A</div>
                  <div style='color:rgba(255,255,255,0.75);font-size:13px;margin-top:2px;'>{subtituloEncabezado}</div>
                </td>
                <td align='right'>
                  <div style='color:rgba(255,255,255,0.5);font-size:11px;'>{DateTime.Now:dd/MM/yyyy HH:mm}</div>
                </td>
              </tr>
            </table>
          </td>
        </tr>
        <tr>
          <td style='background:#ffffff;padding:32px;'>
            <p style='margin:0 0 20px;font-size:15px;color:#374151;'>Hola <strong>{nombreUsuario}</strong>,</p>
            {contenidoTarjeta}
            {piePagina}
            <p style='margin:28px 0 0;font-size:12px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:16px;'>
              Mensaje automático del Sistema Jhomeron S.A. No responder este correo.
            </p>
          </td>
        </tr>
        <tr>
          <td style='background:#1e3a5f;padding:14px 32px;text-align:center;'>
            <span style='color:rgba(255,255,255,0.5);font-size:11px;'>© {DateTime.Now.Year} Jhomeron S.A</span>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body></html>";
        }

        private static string Tarjeta(string contenido) =>
            $"<div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:20px 24px;margin:16px 0;font-size:14px;color:#374151;line-height:1.8;'>{contenido}</div>";

        private static string Fila(string label, string valor) =>
            $"<div style='margin-bottom:10px;'><span style='color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:0.05em;'>{label}</span><br><strong style='color:#111827;font-size:15px;'>{valor}</strong></div>";

        // ── ALERTA ESTADO DE MANTENIMIENTO ───────────────────────
        public async Task EnviarAlertaEstadoMantenimientoAsync(
            string destinatario, string nombreUsuario,
            string placa, string tipoMantenimiento,
            string estado, DateTime fecha)
        {
            string asunto, colorEnc, icono, subtitulo, badgeBg, badgeColor;

            switch (estado)
            {
                case "En proceso":
                    asunto     = $"🔧 Mantenimiento EN PROCESO — {placa}";
                    colorEnc   = "#b45309"; icono = "🔧";
                    subtitulo  = "Mantenimiento iniciado";
                    badgeBg    = "#fef3c7"; badgeColor = "#92400e";
                    break;
                case "Culminado":
                    asunto     = $"✅ Mantenimiento CULMINADO — {placa}";
                    colorEnc   = "#15803d"; icono = "✅";
                    subtitulo  = "Mantenimiento finalizado";
                    badgeBg    = "#dcfce7"; badgeColor = "#166534";
                    break;
                default: // Pendiente / registrado
                    asunto     = $"📅 Mantenimiento registrado — {placa}";
                    colorEnc   = "#1e3a5f"; icono = "📅";
                    subtitulo  = "Nuevo mantenimiento programado";
                    badgeBg    = "#dbeafe"; badgeColor = "#1e40af";
                    break;
            }

            var tarjeta = Tarjeta(
                Fila("Vehículo", placa) +
                Fila("Tipo de mantenimiento", tipoMantenimiento) +
                Fila("Fecha programada", fecha.ToString("dddd dd/MM/yyyy")) +
                $"<div style='margin-top:4px;'><span style='color:#6b7280;font-size:12px;text-transform:uppercase;'>Estado</span><br>" +
                $"<span style='background:{badgeBg};color:{badgeColor};padding:4px 12px;border-radius:20px;font-size:13px;font-weight:700;'>{estado}</span></div>");

            await EnviarAsync(destinatario, asunto, PlantillaBase(colorEnc, icono, subtitulo, nombreUsuario, tarjeta));
        }

        // Mantener compatibilidad con llamadas antiguas
        public async Task EnviarAlertaMantenimientoAsync(
            string destinatario, string nombreUsuario,
            string placa, string tipoMantenimiento,
            DateTime fechaProgramada, int idMante)
            => await EnviarAlertaEstadoMantenimientoAsync(
                destinatario, nombreUsuario, placa, tipoMantenimiento, "Pendiente", fechaProgramada);

        // ── ALERTA VENCIMIENTO ───────────────────────────────────
        public async Task EnviarAlertaVencimientoAsync(
            string destinatario, string nombreUsuario,
            string tipoDocumento, string placa,
            DateTime fechaVencimiento, int diasRestantes)
        {
            string asunto, colorEnc, icono, subtitulo, badgeBg, badgeColor, estadoTexto, accion;

            if (diasRestantes < 0)
            {
                asunto      = $"🔴 {tipoDocumento} VENCIDO — {placa}";
                colorEnc    = "#7f1d1d"; icono = "🔴";
                subtitulo   = $"Vencido hace {Math.Abs(diasRestantes)} día(s)";
                badgeBg     = "#fee2e2"; badgeColor = "#991b1b";
                estadoTexto = $"VENCIDO el {fechaVencimiento:dd/MM/yyyy}";
                accion      = "Este documento ya venció. Renuévalo a la brevedad para evitar inconvenientes.";
            }
            else if (diasRestantes == 0)
            {
                asunto      = $"🚨 {tipoDocumento} vence HOY — {placa}";
                colorEnc    = "#b45309"; icono = "🚨";
                subtitulo   = "Vence HOY";
                badgeBg     = "#fef3c7"; badgeColor = "#92400e";
                estadoTexto = $"Vence HOY {fechaVencimiento:dd/MM/yyyy}";
                accion      = "Este documento vence hoy. Gestiona la renovación de inmediato.";
            }
            else
            {
                asunto      = $"⚠️ {tipoDocumento} vence en {diasRestantes} día(s) — {placa}";
                colorEnc    = "#1e3a5f"; icono = "⚠️";
                subtitulo   = $"Vence en {diasRestantes} día(s)";
                badgeBg     = "#dbeafe"; badgeColor = "#1e40af";
                estadoTexto = $"Vence en {diasRestantes} día(s) — {fechaVencimiento:dd/MM/yyyy}";
                accion      = "Te recomendamos gestionar la renovación cuanto antes.";
            }

            var tarjeta = Tarjeta(
                Fila("Vehículo", placa) +
                Fila("Documento", tipoDocumento) +
                Fila("Fecha de vencimiento", fechaVencimiento.ToString("dddd dd/MM/yyyy")) +
                $"<div style='margin-top:4px;'><span style='color:#6b7280;font-size:12px;text-transform:uppercase;'>Estado</span><br>" +
                $"<span style='background:{badgeBg};color:{badgeColor};padding:4px 12px;border-radius:20px;font-size:13px;font-weight:700;'>{estadoTexto}</span></div>");

            var pie = $"<p style='color:#374151;font-size:14px;margin:16px 0 0;'>{accion}</p>";
            await EnviarAsync(destinatario, asunto, PlantillaBase(colorEnc, icono, subtitulo, nombreUsuario, tarjeta, pie));
        }
    }
}
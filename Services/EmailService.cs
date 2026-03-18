using System.Net;
using System.Net.Mail;

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

        public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            // Fire and forget — no bloquea la petición web
            _ = Task.Run(async () =>
            {
                try
                {
                    var host     = _config["Email:Host"]     ?? "mail.jhomeron.com";
                    var port     = int.Parse(_config["Email:Port"] ?? "995");
                    var usuario  = _config["Email:Usuario"]  ?? "soporte@jhomeron.com";
                    var password = _config["Email:Password"] ?? "Sinformatico0-0";

                    using var cliente = new SmtpClient(host, port)
                    {
                        EnableSsl      = true,
                        Credentials    = new NetworkCredential(usuario, password),
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout        = 30000
                    };

                    using var mensaje = new MailMessage
                    {
                        From       = new MailAddress(usuario, "Sistema Jhomeron"),
                        Subject    = asunto,
                        Body       = cuerpoHtml,
                        IsBodyHtml = true
                    };
                    mensaje.To.Add(destinatario);

                    await cliente.SendMailAsync(mensaje);
                    _logger.LogInformation("Email enviado a {dest}: {asunto}", destinatario, asunto);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error enviando email a {dest}: {msg}", destinatario, ex.Message);
                }
            });

            await Task.CompletedTask;
        }

        public async Task EnviarAlertaMantenimientoAsync(
            string destinatario,
            string nombreUsuario,
            string placa,
            string tipoMantenimiento,
            DateTime fechaProgramada,
            int idMante)
        {
            var asunto = $"Mantenimiento programado para hoy — {placa}";
            var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#1a3a6b;padding:20px;border-radius:8px 8px 0 0;'>
    <h2 style='color:white;margin:0;'>Sistema Jhomeron S.A</h2>
    <p style='color:#adc4e8;margin:4px 0 0;'>Alerta de mantenimiento</p>
  </div>
  <div style='background:#f8f9fa;padding:24px;border:1px solid #e0e0e0;border-top:none;border-radius:0 0 8px 8px;'>
    <p>Hola <strong>{nombreUsuario}</strong>,</p>
    <p>Tienes un mantenimiento programado para <strong>hoy {fechaProgramada:dd/MM/yyyy}</strong>:</p>
    <div style='background:white;border:1px solid #e0e0e0;border-radius:6px;padding:16px;margin:16px 0;'>
      <p style='margin:4px 0;'><strong>Vehículo:</strong> {placa}</p>
      <p style='margin:4px 0;'><strong>Tipo:</strong> {tipoMantenimiento}</p>
      <p style='margin:4px 0;'><strong>Fecha programada:</strong> {fechaProgramada:dd/MM/yyyy}</p>
    </div>
    <p>Ingresa al sistema y dale a <strong>Proceder</strong> cuando el mantenimiento haya comenzado.</p>
    <p style='color:#666;font-size:0.85em;margin-top:24px;'>Este es un mensaje automatico del sistema Jhomeron S.A.</p>
  </div>
</div>";

            await EnviarAsync(destinatario, asunto, cuerpo);
        }
    }
}
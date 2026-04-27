using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace PROYJHOME2026.Services
{
    public class TwilioService
    {
        private readonly IConfiguration         _config;
        private readonly ILogger<TwilioService> _logger;

        private static readonly HashSet<string> _enviados   = new();
        private static DateTime                  _fechaCache = DateTime.Today;

        public TwilioService(IConfiguration config, ILogger<TwilioService> logger)
        {
            _config = config;
            _logger = logger;
            var sid   = _config["Twilio:AccountSid"]  ?? "";
            var token = _config["Twilio:AuthToken"]   ?? "";
            if (!string.IsNullOrEmpty(sid) && !string.IsNullOrEmpty(token))
                TwilioClient.Init(sid, token);
        }

        public async Task EnviarATodosAsync(string claveMensaje, string texto)
        {
            if (_fechaCache != DateTime.Today) { _enviados.Clear(); _fechaCache = DateTime.Today; }

            var destinatarios = _config.GetSection("Twilio:Destinatarios").Get<List<DestinatarioTwilio>>();
            if (destinatarios == null || destinatarios.Count == 0)
            {
                _logger.LogWarning("Twilio: no hay destinatarios configurados");
                return;
            }
            var numeroSandbox = _config["Twilio:NumeroSandbox"] ?? "+14155238886";
            foreach (var dest in destinatarios)
            {
                var clave = $"{dest.Numero}|{claveMensaje}";
                if (_enviados.Contains(clave)) continue;
                await EnviarAsync(dest.Numero, dest.Nombre, numeroSandbox, texto);
                _enviados.Add(clave);
            }
        }

        private async Task EnviarAsync(string numero, string nombre, string desde, string texto)
        {
            try
            {
                var msg = await MessageResource.CreateAsync(
                    to:   new Twilio.Types.PhoneNumber($"whatsapp:+51{numero}"),
                    from: new Twilio.Types.PhoneNumber($"whatsapp:{desde}"),
                    body: texto);
                _logger.LogInformation("WhatsApp enviado a {nombre} — SID: {sid}", nombre, msg.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Twilio: error enviando a {nombre}", nombre);
            }
        }
    }

    public class DestinatarioTwilio
    {
        public string Nombre { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
    }
}
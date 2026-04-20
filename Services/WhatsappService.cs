using System.Net;
using System.Text;

namespace PROYJHOME2026.Services
{
    /// <summary>
    /// Envía mensajes de WhatsApp vía CallMeBot (gratuito).
    /// Requisito previo: cada número debe activar el bot una sola vez
    /// enviando "I allow callmebot to send me messages" al +34 644 597 490.
    /// Luego CallMeBot responde con el apikey personal de ese número.
    /// Configurar en appsettings.json → "WhatsApp:Destinatarios"
    /// </summary>
    public class WhatsAppService
    {
        private readonly IConfiguration          _config;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly HttpClient               _http;

        // Evitar spam: registra qué mensajes ya se enviaron hoy (clave = numero+asunto)
        private static readonly HashSet<string> _enviados = new();
        private static DateTime _fechaCache = DateTime.Today;

        public WhatsAppService(IConfiguration config, ILogger<WhatsAppService> logger, IHttpClientFactory httpFactory)
        {
            _config = config;
            _logger = logger;
            _http   = httpFactory.CreateClient("callmebot");
        }

        /// <summary>
        /// Envía un mensaje a todos los destinatarios configurados en appsettings.
        /// Incluye protección anti-duplicado por día.
        /// </summary>
        public async Task EnviarATodosAsync(string claveMensaje, string texto)
        {
            // Resetear caché si cambió el día
            if (_fechaCache != DateTime.Today)
            {
                _enviados.Clear();
                _fechaCache = DateTime.Today;
            }

            var destinatarios = _config.GetSection("WhatsApp:Destinatarios").Get<List<DestinatarioWsp>>();
            if (destinatarios == null || destinatarios.Count == 0)
            {
                _logger.LogWarning("WhatsApp: no hay destinatarios configurados en appsettings.json");
                return;
            }

            foreach (var dest in destinatarios)
            {
                var clave = $"{dest.Numero}|{claveMensaje}";
                if (_enviados.Contains(clave))
                {
                    _logger.LogDebug("WhatsApp: ya enviado hoy a {num} — {clave}", dest.Numero, claveMensaje);
                    continue;
                }

                await EnviarAsync(dest.Numero, dest.ApiKey, texto);
                _enviados.Add(clave);
            }
        }

        private async Task EnviarAsync(string numero, string apiKey, string texto)
        {
            try
            {
                // CallMeBot API: GET con parámetros urlencoded
                var textoCodificado = WebUtility.UrlEncode(texto);
                var url = $"https://api.callmebot.com/whatsapp.php?phone={numero}&text={textoCodificado}&apikey={apiKey}";

                var respuesta = await _http.GetAsync(url);
                var contenido = await respuesta.Content.ReadAsStringAsync();

                if (respuesta.IsSuccessStatusCode)
                    _logger.LogInformation("WhatsApp enviado a {num} ✓", numero);
                else
                    _logger.LogWarning("WhatsApp a {num} — respuesta {status}: {body}", numero, respuesta.StatusCode, contenido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WhatsApp: error enviando a {num}", numero);
            }
        }
    }

    public class DestinatarioWsp
    {
        /// <summary>Número con código de país, sin +. Ej: 51987654321</summary>
        public string Numero { get; set; } = string.Empty;

        /// <summary>ApiKey que CallMeBot le envió al número al activarse.</summary>
        public string ApiKey { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;
    }
}
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using System.Text;
using System.Text.Json;

namespace PROYJHOME2026.Services
{
    public class IAService
    {
        private readonly AppDbContext       _context;
        private readonly IConfiguration    _config;
        
        private readonly ILogger<IAService> _logger;

        private const string ApiUrl = "https://api.anthropic.com/v1/messages";
        private const string Model  = "claude-sonnet-4-5";
        private const int    MaxMensajesContexto       = 20;
        private const int    MaxConversacionesHistorial = 5;

        public IAService(AppDbContext context, IConfiguration config, ILogger<IAService> logger)
        {
            _context = context;
            _config  = config;
            _logger  = logger;
        }

        // ── SYSTEM PROMPT ─────────────────────────────────────────
        public async Task<string> ConstruirSystemPromptAsync(Usuario usuario)
        {
            var rol     = usuario.rol;
            var nombre  = usuario.nombreCompleto ?? usuario.username;
            var esAdmin = rol == "Admin" || usuario.username == "danitza";
            var sb      = new StringBuilder();

            sb.AppendLine("Eres un asistente de análisis de datos del Sistema de Gestión Jhomeron S.A.");
            sb.AppendLine($"Usuario: {nombre} | Rol: {rol}");
            sb.AppendLine();
            sb.AppendLine("## REGLAS");
            sb.AppendLine("- Solo lectura. NUNCA sugieras eliminar, modificar ni crear datos.");
            sb.AppendLine("- Responde SIEMPRE en español, de forma concisa y profesional.");
            sb.AppendLine("- Si el usuario pide algo fuera de su área, indícalo amablemente.");
            sb.AppendLine("- Cuando el usuario pida exportar, pregunta qué campos quiere y si prefiere PDF o Excel.");
            sb.AppendLine("- Los gráficos NO se pueden exportar, solo datos tabulares.");
            sb.AppendLine("- Si la consulta es ambigua, haz una sola pregunta de aclaración.");
            sb.AppendLine();

            // Datos actuales embebidos en el system prompt
            sb.AppendLine("## DATOS ACTUALES DEL SISTEMA");
            sb.Append(await ObtenerResumenDatosAsync(usuario));
            sb.AppendLine();

            sb.AppendLine("## ÁREAS DISPONIBLES PARA ESTE USUARIO");
            if (esAdmin)
                sb.AppendLine("Acceso total: TI, Flota vehicular, Producción, Logística, SSOMA, RR.HH.");
            else if (rol == "SoporteTI")
                sb.AppendLine("Solo TI: equipos (PC Completo, Laptop, Monitor, Mouse, Teclado, Impresora, UPS, Switch, Router, Proyector) y asignaciones. Sin acceso a chips, carros, máquinas.");
            else if (rol == "Transporte")
                sb.AppendLine("Solo Flota vehicular: carros, mantenimientos, seguros, modalidades, habilitaciones. Sin acceso a TI, máquinas, chips.");
            else if (rol == "Produccion")
                sb.AppendLine("Solo Producción: máquinas y asignaciones de máquinas. Sin acceso a TI, carros, chips.");
            else if (rol == "SSOMA")
                sb.AppendLine("Solo SSOMA: botiquines, extintores, checklist de transporte, inspecciones. Sin acceso a TI, máquinas, chips, datos de empleados.");
            else if (rol == "Logistica")
                sb.AppendLine("Solo Logística: chips/SIM. Sin acceso a TI, carros, máquinas.");

            sb.AppendLine();
            sb.AppendLine("## FORMATO DE RESPUESTA");
            sb.AppendLine("Responde SIEMPRE en JSON puro (sin bloques de código markdown):");
            sb.AppendLine("{");
            sb.AppendLine("  \"respuesta\": \"Texto en markdown con la respuesta\",");
            sb.AppendLine("  \"recomendacion\": \"Una recomendación breve y accionable\",");
            sb.AppendLine("  \"grafico\": {");
            sb.AppendLine("    \"tipo\": \"barras|dona|lineal|ninguno\",");
            sb.AppendLine("    \"titulo\": \"Título del gráfico\",");
            sb.AppendLine("    \"labels\": [\"A\", \"B\"],");
            sb.AppendLine("    \"datos\": [10, 20],");
            sb.AppendLine("    \"colores\": [\"#3b82f6\",\"#10b981\",\"#f59e0b\",\"#ef4444\",\"#8b5cf6\",\"#06b6d4\",\"#f97316\"]");
            sb.AppendLine("  },");
            sb.AppendLine("  \"tieneExportacion\": false,");
            sb.AppendLine("  \"datosExportacion\": null");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("Reglas de gráfico: 'barras' para comparar cantidades, 'dona' para proporciones/estados, 'lineal' para tendencias en el tiempo, 'ninguno' si no aplica.");
            sb.AppendLine("Si hay datos exportables, pon tieneExportacion: true y completa datosExportacion con {titulo, columnas: [...], filas: [[...], ...]}.");

            return sb.ToString();
        }

        // ── RESUMEN DE DATOS (va en el system prompt) ─────────────
        private async Task<string> ObtenerResumenDatosAsync(Usuario usuario)
        {
            var sb      = new StringBuilder();
            var rol     = usuario.rol;
            var esAdmin = rol == "Admin" || usuario.username == "danitza";

            if (esAdmin || rol == "SoporteTI")
            {
                var equipos = await _context.Equipos.Include(e => e.TipoEquipo).ToListAsync();
                var asigs   = await _context.Asignaciones
                    .Include(a => a.Empleado).Include(a => a.Equipo).ThenInclude(e => e!.TipoEquipo)
                    .Where(a => a.EstadoAsignacion == "Activo").ToListAsync();

                sb.AppendLine($"### TI — {equipos.Count} equipos totales");
                foreach (var g in equipos.GroupBy(e => e.estado_equipo))
                    sb.AppendLine($"- {g.Key}: {g.Count()}");
                foreach (var g in equipos.GroupBy(e => e.TipoEquipo?.tipo ?? "Sin tipo"))
                    sb.AppendLine($"- Tipo {g.Key}: {g.Count()}");
                sb.AppendLine($"- Asignaciones activas: {asigs.Count}");

                sb.AppendLine("Detalle equipos:");
                foreach (var e in equipos.Take(80))
                    sb.AppendLine($"  {e.NombrePc ?? e.marca ?? "—"} | {e.TipoEquipo?.tipo ?? "—"} | {e.estado_equipo} | Serie: {e.numero_serie ?? "—"} | {e.marca ?? "—"} {e.modelo ?? "—"}");

                sb.AppendLine("Asignaciones activas:");
                foreach (var a in asigs.Take(50))
                    sb.AppendLine($"  {a.Equipo?.NombrePc ?? a.Equipo?.marca ?? "—"} → {a.Empleado?.nombre} {a.Empleado?.paterno} ({a.FechaAsignacion:dd/MM/yyyy})");
            }

            if (esAdmin || rol == "Logistica")
            {
                var chips = await _context.Chips.Include(c => c.Asignaciones).ToListAsync();
                var asig  = chips.Count(c => c.Asignaciones.Any(a => a.EstadoAsignacion == "Activo"));
                sb.AppendLine($"\n### Logística — {chips.Count} chips/SIM");
                sb.AppendLine($"- Asignados: {asig} | Sin asignar: {chips.Count - asig}");
                foreach (var c in chips.Take(50))
                    sb.AppendLine($"  {c.NumeroCelular} | {(c.Asignaciones.Any(a => a.EstadoAsignacion == "Activo") ? "Asignado" : "Libre")}");
            }

            if (esAdmin || rol == "Transporte")
            {
                var carros = await _context.Carros.ToListAsync();
                var mantes = await _context.MantenimientosCarros
                    .Include(m => m.TipoMantenimiento).Include(m => m.Carro).ToListAsync();
                var seguros    = await _context.CarroSeguros.Include(cs => cs.Carro).Include(cs => cs.Seguro).ToListAsync();
                var modalidades= await _context.CarroModalidades.Include(cm => cm.Carro).Include(cm => cm.Modalidad).ToListAsync();

                sb.AppendLine($"\n### Flota Vehicular — {carros.Count} carros");
                foreach (var g in carros.GroupBy(c => c.Estado))
                    sb.AppendLine($"- {g.Key}: {g.Count()}");
                foreach (var c in carros)
                    sb.AppendLine($"  {c.Placa} | {c.Marca} {c.Modelo} | {c.Estado} | {c.Color ?? "—"}");

                sb.AppendLine($"Mantenimientos: {mantes.Count} (Pendientes: {mantes.Count(m => m.Estado == "Pendiente")}, En proceso: {mantes.Count(m => m.Estado == "En proceso")})");
                foreach (var m in mantes.Take(40))
                    sb.AppendLine($"  {m.Carro?.Placa} | {m.TipoMantenimiento?.Nombre} | {m.Estado} | {m.FechaProgramada:dd/MM/yyyy}");

                sb.AppendLine($"Seguros: {seguros.Count}");
                foreach (var s in seguros.Take(20))
                    sb.AppendLine($"  {s.Carro?.Placa} | {s.Seguro?.TipoSeguro} | Vence: {s.FechaCulminada?.ToString("dd/MM/yyyy") ?? "—"}");

                sb.AppendLine($"Modalidades: {modalidades.Count}");
                foreach (var m in modalidades.Take(20))
                    sb.AppendLine($"  {m.Carro?.Placa} | {m.Modalidad?.TipoModalidad} | Vence: {m.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "—"}");
            }

            if (esAdmin || rol == "Produccion")
            {
                var maquinas = await _context.Maquinas
                    .Include(m => m.Asignaciones).ThenInclude(a => a.Encargados).ThenInclude(e => e.Empleado)
                    .ToListAsync();
                sb.AppendLine($"\n### Producción — {maquinas.Count} máquinas");
                foreach (var g in maquinas.GroupBy(m => m.Estado))
                    sb.AppendLine($"- {g.Key}: {g.Count()}");
                foreach (var m in maquinas)
                {
                    var encs = m.AsignacionActual?.Encargados
                        .Select(e => $"{e.Empleado?.nombre} {e.Empleado?.paterno}").ToList() ?? new();
                    sb.AppendLine($"  {m.NumeroCompleto} | {m.NombreMaquina} | {m.Marca ?? "—"} | {m.Estado} | Enc: {(encs.Any() ? string.Join(", ", encs) : "Sin asignar")}");
                }
            }

            if (esAdmin || rol == "SSOMA")
            {
                var ases = await _context.CarroAsesorios.Include(ca => ca.Asesorio).Include(ca => ca.Carro).ToListAsync();
                var bots = ases.Where(a => a.Asesorio?.TipoAsesorio?.Contains("Botiquín") == true || a.Asesorio?.TipoAsesorio?.Contains("Botiquin") == true).ToList();
                var exts = ases.Where(a => a.Asesorio?.TipoAsesorio?.Contains("Extintor") == true).ToList();
                sb.AppendLine($"\n### SSOMA — Botiquines: {bots.Count} | Extintores: {exts.Count}");
                foreach (var e in exts.Take(25))
                    sb.AppendLine($"  Extintor: {e.Carro?.Placa} | {e.TipoExtintor ?? "—"} | Vence: {e.FechaVencimientoExtintor?.ToString("dd/MM/yyyy") ?? "—"}");
                var inspB = await _context.InspeccionBotiquinTransportes.CountAsync();
                var inspE = await _context.InspeccionExtintores.CountAsync();
                sb.AppendLine($"  Inspecciones botiquines: {inspB} | Inspecciones extintores: {inspE}");
            }

            if (esAdmin)
            {
                var emps   = await _context.Empleados.ToListAsync();
                var grupos = await _context.Grupos.ToListAsync();
                sb.AppendLine($"\n### RR.HH — {emps.Count} empleados | {grupos.Count} grupos");
                foreach (var g in emps.GroupBy(e => e.estado ?? "Sin estado"))
                    sb.AppendLine($"- {g.Key}: {g.Count()}");
                foreach (var g in emps.GroupBy(e => e.Cargo ?? "Sin cargo"))
                    sb.AppendLine($"- Cargo {g.Key}: {g.Count()}");
                foreach (var e in emps.Take(60))
                    sb.AppendLine($"  {e.nombre} {e.paterno} {e.materno ?? ""} | DNI: {e.dni ?? "—"} | Cargo: {e.Cargo ?? "—"} | Estado: {e.estado ?? "—"} | Correo: {e.correo ?? "—"} | Dirección: {e.direccion ?? "—"}");
            }

            return sb.ToString();
        }

        // ── ENVIAR MENSAJE ────────────────────────────────────────
        public async Task<IAResponseDto> EnviarMensajeAsync(
            int idConversacion, string mensajeUsuario, Usuario usuario)
        {
            var apiKey = _config["Anthropic:ApiKey"] ?? "";

            // FIX: ToListAsync() primero, luego TakeLast en memoria
            var todosLosMensajes = await _context.IAMensajes
                .Where(m => m.IdConversacion == idConversacion)
                .OrderBy(m => m.FechaCreacion)
                .ToListAsync();

            var historial = todosLosMensajes.TakeLast(MaxMensajesContexto).ToList();

            var mensajes = new List<object>();
            foreach (var msg in historial)
                mensajes.Add(new { role = msg.Rol, content = msg.Contenido });

            mensajes.Add(new { role = "user", content = mensajeUsuario });

            // System prompt con datos embebidos
            var systemPrompt = await ConstruirSystemPromptAsync(usuario);

            var payload = new
            {
                model      = Model,
                max_tokens = 4096,
                system     = systemPrompt,
                messages   = mensajes
            };

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var http = new HttpClient(handler);
            http.Timeout   = TimeSpan.FromSeconds(90);
            http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var json     = JsonSerializer.Serialize(payload);
            var content  = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync(ApiUrl, content);
            var body     = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API error {code}: {body}", response.StatusCode, body);
                throw new Exception($"Error de comunicación con la IA ({response.StatusCode})");
            }

            var doc      = JsonDocument.Parse(body);
            var textoRaw = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text").GetString() ?? "{}";

            textoRaw = textoRaw.Trim();
            if (textoRaw.StartsWith("```json")) textoRaw = textoRaw[7..];
            if (textoRaw.StartsWith("```"))     textoRaw = textoRaw[3..];
            if (textoRaw.EndsWith("```"))        textoRaw = textoRaw[..^3];
            textoRaw = textoRaw.Trim();

            IAResponseDto resultado;
            try
            {
                resultado = JsonSerializer.Deserialize<IAResponseDto>(textoRaw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new IAResponseDto { Respuesta = textoRaw };
            }
            catch
            {
                resultado = new IAResponseDto { Respuesta = textoRaw };
            }

            // Guardar en BD
            _context.IAMensajes.Add(new IAMensaje
            {
                IdConversacion = idConversacion,
                Rol            = "user",
                Contenido      = mensajeUsuario,
                FechaCreacion  = DateTime.Now
            });
            _context.IAMensajes.Add(new IAMensaje
            {
                IdConversacion       = idConversacion,
                Rol                  = "assistant",
                Contenido            = resultado.Respuesta ?? "",
                GraficoJson          = resultado.Grafico != null ? JsonSerializer.Serialize(resultado.Grafico) : null,
                Recomendacion        = resultado.Recomendacion,
                TieneExportacion     = resultado.TieneExportacion,
                DatosExportacionJson = resultado.DatosExportacion != null ? JsonSerializer.Serialize(resultado.DatosExportacion) : null,
                FechaCreacion        = DateTime.Now.AddSeconds(1)
            });

            var conv = await _context.IAConversaciones.FindAsync(idConversacion);
            if (conv != null)
            {
                conv.FechaUltimoMensaje = DateTime.Now;
                if (string.IsNullOrEmpty(conv.Titulo))
                    conv.Titulo = mensajeUsuario.Length > 60 ? mensajeUsuario[..57] + "..." : mensajeUsuario;
            }
            await _context.SaveChangesAsync();
            return resultado;
        }

        // ── NUEVA CONVERSACIÓN ────────────────────────────────────
        public async Task<IAConversacion> NuevaConversacionAsync(int idUsuario)
        {
            var activa = await _context.IAConversaciones
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario && c.EsActiva);
            if (activa != null) activa.EsActiva = false;

            var todas = await _context.IAConversaciones
                .Where(c => c.IdUsuario == idUsuario)
                .OrderByDescending(c => c.FechaUltimoMensaje)
                .ToListAsync();

            if (todas.Count >= MaxConversacionesHistorial)
            {
                var masAntigua = todas.Last();
                _context.IAMensajes.RemoveRange(
                    _context.IAMensajes.Where(m => m.IdConversacion == masAntigua.IdConversacion));
                _context.IAConversaciones.Remove(masAntigua);
            }

            var nueva = new IAConversacion
            {
                IdUsuario          = idUsuario,
                FechaCreacion      = DateTime.Now,
                FechaUltimoMensaje = DateTime.Now,
                EsActiva           = true
            };
            _context.IAConversaciones.Add(nueva);
            await _context.SaveChangesAsync();
            return nueva;
        }

        // ── CERRAR SESIÓN ─────────────────────────────────────────
        public async Task CerrarSesionAsync(int idUsuario)
        {
            var activa = await _context.IAConversaciones
                .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario && c.EsActiva);
            if (activa != null) { activa.EsActiva = false; await _context.SaveChangesAsync(); }
        }

        // ── DASHBOARD ─────────────────────────────────────────────
        public async Task<IaDashboardDto> ObtenerDashboardAsync(Usuario usuario)
        {
            var rol     = usuario.rol;
            var esAdmin = rol == "Admin" || usuario.username == "danitza";
            var dto     = new IaDashboardDto { Area = ObtenerNombreArea(usuario) };

            if (esAdmin || rol == "SoporteTI")
            {
                var equipos = await _context.Equipos.ToListAsync();
                dto.TarjetasTI = equipos
                    .GroupBy(e => e.estado_equipo)
                    .Select(g => new TarjetaConteo { Label = g.Key, Valor = g.Count() })
                    .ToList();
                dto.TotalTI = equipos.Count;
            }

            if (esAdmin || rol == "Transporte")
            {
                var carros = await _context.Carros.ToListAsync();
                dto.TarjetasFlota = carros
                    .GroupBy(c => c.Estado)
                    .Select(g => new TarjetaConteo { Label = g.Key, Valor = g.Count() })
                    .ToList();
                dto.TotalFlota = carros.Count;
                dto.MantenimientosPendientes = await _context.MantenimientosCarros.CountAsync(m => m.Estado == "Pendiente");
                dto.MantenimientosEnProceso  = await _context.MantenimientosCarros.CountAsync(m => m.Estado == "En proceso");
            }

            if (esAdmin || rol == "Produccion")
            {
                var maquinas = await _context.Maquinas.ToListAsync();
                dto.TarjetasProduccion = maquinas
                    .GroupBy(m => m.Estado)
                    .Select(g => new TarjetaConteo { Label = g.Key, Valor = g.Count() })
                    .ToList();
                dto.TotalProduccion = maquinas.Count;
            }

            if (esAdmin || rol == "Logistica")
            {
                var chips = await _context.Chips.Include(c => c.Asignaciones).ToListAsync();
                var asig  = chips.Count(c => c.Asignaciones.Any(a => a.EstadoAsignacion == "Activo"));
                dto.TarjetasLogistica = new List<TarjetaConteo>
                {
                    new() { Label = "Asignados",   Valor = asig },
                    new() { Label = "Sin asignar", Valor = chips.Count - asig }
                };
                dto.TotalLogistica = chips.Count;
            }

            if (esAdmin || rol == "SSOMA")
            {
                var ases = await _context.CarroAsesorios.Include(ca => ca.Asesorio).ToListAsync();
                dto.TotalBotiquines = ases.Count(a => a.Asesorio?.TipoAsesorio?.Contains("Botiquín") == true || a.Asesorio?.TipoAsesorio?.Contains("Botiquin") == true);
                dto.TotalExtintores = ases.Count(a => a.Asesorio?.TipoAsesorio?.Contains("Extintor") == true);
            }

            if (esAdmin)
            {
                dto.TotalEmpleados   = await _context.Empleados.CountAsync();
                dto.EmpleadosActivos = await _context.Empleados.CountAsync(e => e.estado == "Activo");
            }

            return dto;
        }

        private static string ObtenerNombreArea(Usuario u) => u.rol switch
        {
            "Admin"      => "Administración General",
            "SoporteTI"  => "Tecnología de la Información",
            "Transporte" => "Flota Vehicular",
            "Produccion" => "Producción",
            "SSOMA"      => "SSOMA",
            "Logistica"  => "Logística",
            _            => u.rol
        };
    }

    // ── DTOs ──────────────────────────────────────────────────────
    public class IAResponseDto
    {
        public string?           Respuesta        { get; set; }
        public string?           Recomendacion    { get; set; }
        public IAGraficoDto?     Grafico          { get; set; }
        public bool              TieneExportacion { get; set; }
        public IAExportacionDto? DatosExportacion { get; set; }
    }

    public class IAGraficoDto
    {
        public string        Tipo    { get; set; } = "ninguno";
        public string?       Titulo  { get; set; }
        public List<string>? Labels  { get; set; }
        public List<double>? Datos   { get; set; }
        public List<string>? Colores { get; set; }
    }

    public class IAExportacionDto
    {
        public string?             Titulo   { get; set; }
        public List<string>?       Columnas { get; set; }
        public List<List<string>>? Filas    { get; set; }
    }

    public class IaDashboardDto
    {
        public string Area                     { get; set; } = "";
        public int    TotalTI                  { get; set; }
        public int    TotalFlota               { get; set; }
        public int    TotalProduccion          { get; set; }
        public int    TotalLogistica           { get; set; }
        public int    TotalBotiquines          { get; set; }
        public int    TotalExtintores          { get; set; }
        public int    TotalEmpleados           { get; set; }
        public int    EmpleadosActivos         { get; set; }
        public int    MantenimientosPendientes { get; set; }
        public int    MantenimientosEnProceso  { get; set; }
        public List<TarjetaConteo> TarjetasTI         { get; set; } = new();
        public List<TarjetaConteo> TarjetasFlota       { get; set; } = new();
        public List<TarjetaConteo> TarjetasProduccion  { get; set; } = new();
        public List<TarjetaConteo> TarjetasLogistica   { get; set; } = new();
    }

    public class TarjetaConteo
    {
        public string Label { get; set; } = "";
        public int    Valor { get; set; }
    }
}
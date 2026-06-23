using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    // ── Conversación (agrupa mensajes) ───────────────────────────
    [Table("IAConversaciones")]
    public class IAConversacion
    {
        [Key]
        public int IdConversacion { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [StringLength(200)]
        public string? Titulo { get; set; } // Se genera del primer mensaje

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaUltimoMensaje { get; set; } = DateTime.Now;

        public bool EsActiva { get; set; } = false; // true = sesión actual

        // Navegación
        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; } = null!;
        public ICollection<IAMensaje> Mensajes { get; set; } = new List<IAMensaje>();
    }

    // ── Mensaje individual ───────────────────────────────────────
    [Table("IAMensajes")]
    public class IAMensaje
    {
        [Key]
        public int IdMensaje { get; set; }

        [Required]
        public int IdConversacion { get; set; }

        /// <summary>user | assistant</summary>
        [Required]
        [StringLength(20)]
        public string Rol { get; set; } = "user";

        [Required]
        public string Contenido { get; set; } = string.Empty;

        /// <summary>JSON serializado del gráfico si aplica</summary>
        public string? GraficoJson { get; set; }

        /// <summary>Recomendación de la IA</summary>
        public string? Recomendacion { get; set; }

        /// <summary>Si este mensaje tiene datos exportables</summary>
        public bool TieneExportacion { get; set; } = false;

        /// <summary>JSON con los datos a exportar si aplica</summary>
        public string? DatosExportacionJson { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey("IdConversacion")]
        public IAConversacion Conversacion { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("EquipoBitacora")]
    public class EquipoBitacora
    {
        [Key]
        public int IdBitacora { get; set; }

        [Required]
        public int IdEquipo { get; set; }

        /// <summary>Estado nuevo al momento del registro (ej: Malogrado, Defectuoso, Mantenimiento, Activo, etc.)</summary>
        [Required, StringLength(100)]
        public string EstadoNuevo { get; set; } = string.Empty;

        /// <summary>Estado anterior antes del cambio.</summary>
        [StringLength(100)]
        public string? EstadoAnterior { get; set; }

        /// <summary>Motivo o descripción del cambio.</summary>
        [Required, StringLength(500)]
        public string Motivo { get; set; } = string.Empty;

        /// <summary>Fecha del evento. Puede ser futura si es mantenimiento programado.</summary>
        [Required]
        public DateTime Fecha { get; set; }

        /// <summary>True si es un mantenimiento programado (fecha futura).</summary>
        public bool EsProgramado { get; set; } = false;

        /// <summary>True si el mantenimiento programado ya fue completado.</summary>
        public bool Completado { get; set; } = false;

        [StringLength(100)]
        public string? RegistradoPor { get; set; }

        public int? IdUsuario { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // ── Navegación ───────────────────────────────────────────
        [ForeignKey("IdEquipo")]
        public Equipo Equipo { get; set; } = null!;
    }
}
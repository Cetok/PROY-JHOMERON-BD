using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    // ── Habilitación Vehicular Especial ──────────────────────────
    [Table("HabilitacionVehicular")]
    public class HabilitacionVehicular
    {
        [Key]
        public int IdHabilitacion { get; set; }

        [Required]
        public int IdCarro { get; set; }

        /// <summary>Vinculado obligatoriamente a una Revisión Técnica (Modalidad).</summary>
        [Required]
        public int IdModalidad { get; set; }

        [Required]
        [StringLength(100)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        public DateTime FechaVigencia { get; set; }

        [Required]
        public DateTime FechaCulminacion { get; set; }

        /// <summary>true = este es el certificado vigente para el carro.</summary>
        public bool EsVigente { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;

        [ForeignKey("IdModalidad")]
        public Modalidad Modalidad { get; set; } = null!;
    }

    // ── Luna Polarizada ──────────────────────────────────────────
    [Table("LunaPolarizada")]
    public class LunaPolarizada
    {
        [Key]
        public int IdLuna { get; set; }

        [Required]
        public int IdCarro { get; set; }

        [Required]
        public DateTime FechaVigencia { get; set; }

        /// <summary>
        /// Obligatorio cuando ya existe un registro previo (explica por qué se genera uno nuevo:
        /// pérdida, renovación, etc.)
        /// </summary>
        [StringLength(500)]
        public string? Comentario { get; set; }

        /// <summary>true = este es el certificado vigente para el carro.</summary>
        public bool EsVigente { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;
    }
}
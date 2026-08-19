using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Chips")]
    public class Chip
    {
        [Key]
        public int IdChip { get; set; }

        [Required]
        [StringLength(20)]
        public string NumeroCelular { get; set; } = string.Empty;

        /// <summary>Activo | Inactivo (dado de baja — número que ya no se usa)</summary>
        public string Estado { get; set; } = "Activo";

        // Navegación
        public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
        public ICollection<ChipLog> Logs { get; set; } = new List<ChipLog>();
    }

    // ── Historial de eventos del chip: asignado, desasignado, dado de baja ──
    [Table("ChipLogs")]
    public class ChipLog
    {
        [Key]
        public int IdLog { get; set; }

        [Required]
        public int IdChip { get; set; }

        [ForeignKey("IdChip")]
        public Chip? Chip { get; set; }

        /// <summary>Asignado | Desasignado | Inactivo</summary>
        [Required]
        [StringLength(30)]
        public string TipoEvento { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Detalle { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? RegistradoPor { get; set; }

        public int? IdUsuario { get; set; }
    }
}
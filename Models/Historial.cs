using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Historiales")]
    public class Historial
    {
        [Key]
        public int IdHistoria { get; set; }

        [Required]
        public int IdAsignacion { get; set; }

        [Required]
        public int IdMotivo { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Navegación
        [ForeignKey("IdAsignacion")]
        public Asignacion Asignacion { get; set; } = null!;

        [ForeignKey("IdMotivo")]
        public Motivo Motivo { get; set; } = null!;
    }
}
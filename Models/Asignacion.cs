using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Asignaciones")]
    public class Asignacion
    {
        [Key]
        public int IdAsignacion { get; set; }

        [Required]
        public int IdEmpleado { get; set; }

        [Required]
        public int IdEquipo { get; set; }

        public int? IdChip { get; set; }

        [Required]
        public DateTime FechaAsignacion { get; set; }

        public DateTime? FechaDevolucion { get; set; }

        [Required]
        [StringLength(50)]
        public string EstadoAsignacion { get; set; } = string.Empty;

        // Navegación
        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;

        [ForeignKey("IdEquipo")]
        public Equipo Equipo { get; set; } = null!;

        [ForeignKey("IdChip")]
        public Chip? Chip { get; set; }

        public ICollection<Historial> Historiales { get; set; } = new List<Historial>();
    }
}
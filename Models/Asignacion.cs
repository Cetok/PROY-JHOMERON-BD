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

        // Correo corporativo asignado junto al equipo (opcional)
        [StringLength(150)]
        public string? CorreoEquipo { get; set; }

        // Número de cargo — relaciona con código de hoja física
        [StringLength(50)]
        public string? NumeroCargo { get; set; }

        // Solo "Activo" o "Inactivo" — se maneja automáticamente
        public string EstadoAsignacion { get; set; } = "Activo";

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
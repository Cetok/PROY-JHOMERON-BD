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

        /// <summary>Grupo/Área al que pertenece esta asignación.</summary>
        public int? IdGrupo { get; set; }

        [Required]
        public DateTime FechaAsignacion { get; set; }

        /// <summary>Fecha en que se devolvió/desactivó la asignación.</summary>
        public DateTime? FechaDevolucion { get; set; }

        [StringLength(150)]
        public string? CorreoEquipo { get; set; }

        [StringLength(100)]
        public string? NumeroCargo { get; set; }

        public string EstadoAsignacion { get; set; } = "Activo";

        /// <summary>Observaciones de la asignación: accesorios, estado, etc.</summary>
        [StringLength(1000)]
        public string? Observacion { get; set; }

        // ── Navegación ───────────────────────────────────────────
        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;

        [ForeignKey("IdEquipo")]
        public Equipo Equipo { get; set; } = null!;

        [ForeignKey("IdChip")]
        public Chip? Chip { get; set; }

        [ForeignKey("IdGrupo")]
        public Grupo? Grupo { get; set; }

        public ICollection<Historial> Historiales { get; set; } = new List<Historial>();
    }
}
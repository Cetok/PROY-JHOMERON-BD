using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    /// <summary>Registra cada cambio de estado de un empleado.</summary>
    [Table("EmpleadoEstadoLog")]
    public class EmpleadoEstadoLog
    {
        [Key]
        public int IdLog { get; set; }

        [Required]
        public int IdEmpleado { get; set; }

        public int? IdUsuario { get; set; }

        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        [Required]
        [StringLength(50)]
        public string EstadoAnterior { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EstadoNuevo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;
    }
}
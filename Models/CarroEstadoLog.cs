using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    /// <summary>
    /// Registra cada cambio de estado de un vehículo:
    /// Activo → De baja, Activo → Inactivo, etc.
    /// </summary>
    [Table("CarroEstadoLog")]
    public class CarroEstadoLog
    {
        [Key]
        public int IdLog { get; set; }

        [Required]
        public int IdCarro { get; set; }

        public int? IdUsuario { get; set; }

        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        [Required]
        [StringLength(50)]
        public string EstadoAnterior { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EstadoNuevo { get; set; } = string.Empty;

        // Motivo del cambio: Robo, Venta, No en uso, Mantenimiento, Recuperado, etc.
        [Required]
        [StringLength(100)]
        public string Motivo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;
    }
}
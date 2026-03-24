using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    /// <summary>Registra cada cambio de conductor de un vehículo.</summary>
    [Table("CarroConductorLog")]
    public class CarroConductorLog
    {
        [Key]
        public int IdLog { get; set; }

        [Required]
        public int IdCarro { get; set; }

        public int? IdUsuario { get; set; }

        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        // Conductor anterior (null = sin conductor)
        public int? IdEmpleadoAnterior { get; set; }

        [StringLength(200)]
        public string? NombreConductorAnterior { get; set; }

        // Conductor nuevo (null = se quitó el conductor)
        public int? IdEmpleadoNuevo { get; set; }

        [StringLength(200)]
        public string? NombreConductorNuevo { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;
    }
}
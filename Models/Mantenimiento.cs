using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("TipoMantenimiento")]
    public class TipoMantenimiento
    {
        [Key]
        public int IdTipoMante { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // Navegación
        public ICollection<MantenimientoCarro> MantenimientosCarros { get; set; } = new List<MantenimientoCarro>();
    }

    // -------------------------------------------------------

    [Table("Mantenimiento_carro")]
    public class MantenimientoCarro
    {
        [Key]
        public int IdMante { get; set; }

        [Required]
        public int IdTipoMante { get; set; }

        public DateTime? FechaMante { get; set; }

        [Required]
        public int IdCarro { get; set; }

        public DateTime? FechaCulminada { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = string.Empty;

        // Navegación
        [ForeignKey("IdTipoMante")]
        public TipoMantenimiento TipoMantenimiento { get; set; } = null!;

        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;
    }
}
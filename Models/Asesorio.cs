using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Asesorios")]
    public class Asesorio
    {
        [Key]
        public int IdAsesorio { get; set; }

        [Required]
        [StringLength(100)]
        public string TipoAsesorio { get; set; } = string.Empty;

        // Navegación
        public ICollection<CarroAsesorio> CarroAsesorios { get; set; } = new List<CarroAsesorio>();
        public ICollection<GrupoAsesorio> GrupoAsesorios { get; set; } = new List<GrupoAsesorio>();
    }

    // -------------------------------------------------------

    [Table("Carro_Asesorio")]
    public class CarroAsesorio
    {
        [Required]
        public int IdCarro { get; set; }

        [Required]
        public int IdAsesorio { get; set; }

        public DateTime? FechaAsignada { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Navegación
        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;

        [ForeignKey("IdAsesorio")]
        public Asesorio Asesorio { get; set; } = null!;
    }

    // -------------------------------------------------------

    [Table("Grupo_Asesorio")]
    public class GrupoAsesorio
    {
        [Required]
        public int IdGrupo { get; set; }

        [Required]
        public int IdAsesorio { get; set; }

        public DateTime? FechaAsignada { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Navegación
        [ForeignKey("IdGrupo")]
        public Grupo Grupo { get; set; } = null!;

        [ForeignKey("IdAsesorio")]
        public Asesorio Asesorio { get; set; } = null!;
    }
}
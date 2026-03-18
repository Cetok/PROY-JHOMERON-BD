using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Modalidades")]
    public class Modalidad
    {
        [Key]
        public int IdModalidad { get; set; }

        // Código de habilitación — determina el tipo automáticamente
        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TipoModalidad { get; set; } = string.Empty;

        public DateTime? FechaVigente { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = string.Empty;

        // Navegación
        public ICollection<CarroModalidad> CarroModalidades { get; set; } = new List<CarroModalidad>();
    }

    // -------------------------------------------------------

    [Table("Carro_Modalidad")]
    public class CarroModalidad
    {
        [Required]
        public int IdCarro { get; set; }

        [Required]
        public int IdModalidad { get; set; }

        // Navegación
        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;

        [ForeignKey("IdModalidad")]
        public Modalidad Modalidad { get; set; } = null!;
    }
}
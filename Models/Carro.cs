using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Carros")]
    public class Carro
    {
        [Key]
        public int IdCarro { get; set; }

        [Required]
        [StringLength(20)]
        public string Placa { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Modelo { get; set; } = string.Empty;

        [StringLength(50)]
        public string? NumeroMotor { get; set; }

        public DateTime? FechaCarro { get; set; }

        public DateTime? FechaCompra { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Peso { get; set; }

        public int? Ejes { get; set; }

        [StringLength(50)]
        public string? Categoria { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CargaUtil { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = string.Empty;

        // Navegación
        public ICollection<EmpleadoCarro> EmpleadosCarros { get; set; } = new List<EmpleadoCarro>();
        public ICollection<CarroSeguro> CarroSeguros { get; set; } = new List<CarroSeguro>();
        public ICollection<CarroAsesorio> CarroAsesorios { get; set; } = new List<CarroAsesorio>();
        public ICollection<CarroModalidad> CarroModalidades { get; set; } = new List<CarroModalidad>();
        public ICollection<MantenimientoCarro> MantenimientosCarros { get; set; } = new List<MantenimientoCarro>();
    }
}
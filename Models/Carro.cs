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

        /// <summary>Almacena 01/01/YYYY — solo se usa el año como "Año de modelo".</summary>
        public DateTime? FechaCarro { get; set; }

        /// <summary>Fecha del título del vehículo (antes FechaCompra).</summary>
        public DateTime? FechaTitulo { get; set; }

        /// <summary>Color del vehículo. Ej: Blanco, Rojo, Azul...</summary>
        [StringLength(50)]
        public string? Color { get; set; }

        /// <summary>Fórmula rodante. Ej: 4x2, 4x4, 6x4...</summary>
        [StringLength(20)]
        public string? FormulaRodante { get; set; }

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
        public ICollection<HabilitacionVehicular> HabilitacionesVehiculares { get; set; } = new List<HabilitacionVehicular>();
        public ICollection<LunaPolarizada> LunasPolarizadas { get; set; } = new List<LunaPolarizada>();
    }
}
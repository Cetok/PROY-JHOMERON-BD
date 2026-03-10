using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Seguro")]
    public class Seguro
    {
        [Key]
        public int IdSeguro { get; set; }

        [Required]
        [StringLength(100)]
        public string TipoSeguro { get; set; } = string.Empty;

        // Navegación
        public ICollection<EmpleadoSeguro> EmpleadoSeguros { get; set; } = new List<EmpleadoSeguro>();
        public ICollection<CarroSeguro> CarroSeguros { get; set; } = new List<CarroSeguro>();
    }

    // -------------------------------------------------------

    [Table("Empleado_Seguro")]
    public class EmpleadoSeguro
    {
        [Required]
        public int IdEmpleado { get; set; }

        [Required]
        public int IdSeguro { get; set; }

        public DateTime? FechaAsignada { get; set; }

        public DateTime? FechaCulminada { get; set; }

        // Navegación
        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;

        [ForeignKey("IdSeguro")]
        public Seguro Seguro { get; set; } = null!;
    }

    // -------------------------------------------------------

    [Table("Carro_Seguro")]
    public class CarroSeguro
    {
        [Required]
        public int IdCarro { get; set; }

        [Required]
        public int IdSeguro { get; set; }

        public DateTime? FechaAsignada { get; set; }

        public DateTime? FechaCulminada { get; set; }

        // Navegación
        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;

        [ForeignKey("IdSeguro")]
        public Seguro Seguro { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Empleados_Carros")]
    public class EmpleadoCarro
    {
        [Required]
        public int IdEmpleado { get; set; }

        [Required]
        public int IdCarro { get; set; }

        // Navegación
        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;

        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;
    }
}
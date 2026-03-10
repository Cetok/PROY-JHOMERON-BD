using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Empleado_Grupo")]
    public class EmpleadoGrupo
    {
        [Required]
        public int IdEmpleado { get; set; }

        [Required]
        public int IdGrupo { get; set; }

        // Navegación
        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;

        [ForeignKey("IdGrupo")]
        public Grupo Grupo { get; set; } = null!;
    }
}
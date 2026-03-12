
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Equipos")]
    public class Equipo
    {
        [Key]
        public int idEquipo { get; set; }

        public int idTipoEquipo { get; set; }
        public TipoEquipo? TipoEquipo { get; set; }

        public string? marca { get; set; }
        public string? modelo { get; set; }
        public string? sistema_operativo { get; set; }
        public string? version { get; set; }
        public string? numero_serie { get; set; }

        // Estado automático — se setea solo al crear ("Activo")
        // y se actualiza desde HistorialesController cuando hay evento de cierre
        public string estado_equipo { get; set; } = "Activo";

        public DateTime fecha_compra { get; set; }

        // ELIMINADO: correo_equipo → ahora está en Asignacion

        // Navegación
        public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
    }
}
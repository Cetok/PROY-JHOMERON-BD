using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PROYJHOME2026.Models
{
    public class Equipo
    {
        [Key]
        public int idEquipo{set;get;}
        public int idTipoEquipo{set;get;}
        [ForeignKey("idTipoEquipo")]
        public TipoEquipo? TipoEquipo {set;get;}
        public string? marca{set;get;}
        public string? modelo{set;get;}
        public string? sistema_operativo{set;get;}
        public string? version {set;get;}
        public string? numero_serie { get; set; }

        public string? estado_equipo { get; set; }

        public DateTime fecha_compra { get; set; }

        public string? correo_equipo { get; set; }

    }
}

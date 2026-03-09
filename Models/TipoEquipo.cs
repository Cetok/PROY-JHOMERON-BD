using System.ComponentModel.DataAnnotations;

namespace PROYJHOME2026.Models
{
    public class TipoEquipo
    {
        [Key]
        public int idTipoEquipo{set;get;}
        public string? tipo{set;get;}
    }
}

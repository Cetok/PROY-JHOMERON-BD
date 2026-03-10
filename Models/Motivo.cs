using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Motivos")]
    public class Motivo
    {
        [Key]
        public int IdMotivo { get; set; }

        [Required]
        [StringLength(100)]
        public string TipoMotivo { get; set; } = string.Empty;

        // Navegación
        public ICollection<Historial> Historiales { get; set; } = new List<Historial>();
    }
}
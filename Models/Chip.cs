using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Chips")]
    public class Chip
    {
        [Key]
        public int IdChip { get; set; }

        [Required]
        [StringLength(20)]
        public string NumeroCelular { get; set; } = string.Empty;

        // Navegación
        public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
    }
}
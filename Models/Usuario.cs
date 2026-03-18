using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int idUsuario { get; set; }

        [Required]
        [StringLength(50)]
        public string username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string passwordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string rol { get; set; } = "Admin";

        [StringLength(100)]
        public string? nombreCompleto { get; set; }

        [StringLength(100)]
        public string? correo { get; set; }

        // Número de celular para SMS (futuro)
        [StringLength(20)]
        public string? numeroCelular { get; set; }

        public bool activo { get; set; } = true;

        public DateTime creadoEn { get; set; } = DateTime.Now;

        public DateTime? ultimoAcceso { get; set; }

        public int intentosFallidos { get; set; } = 0;

        public DateTime? bloqueadoHasta { get; set; }
    }
}
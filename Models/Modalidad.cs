using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Modalidades")]
    public class Modalidad
    {
        [Key]
        public int IdModalidad { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TipoModalidad { get; set; } = string.Empty;

        public DateTime? FechaVigente { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = string.Empty;

        public ICollection<CarroModalidad> CarroModalidades { get; set; } = new List<CarroModalidad>();
    }

    [Table("Carro_Modalidad")]
    public class CarroModalidad
    {
        [Required]
        public int IdCarro { get; set; }

        [Required]
        public int IdModalidad { get; set; }

        // Fecha en que se asignó esta modalidad al vehículo
        public DateTime? FechaAsignacion { get; set; }

        // Fecha de vencimiento (~6 meses)
        public DateTime? FechaVencimiento { get; set; }

        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;

        [ForeignKey("IdModalidad")]
        public Modalidad Modalidad { get; set; } = null!;
    }

    // ── Historial de cambios de modalidad por vehículo ───────
    [Table("CarroModalidadLog")]
    public class CarroModalidadLog
    {
        [Key]
        public int IdLog { get; set; }

        [Required]
        public int IdCarro { get; set; }

        [Required]
        public int IdModalidad { get; set; }

        [StringLength(200)]
        public string? TipoModalidad { get; set; }

        [StringLength(20)]
        public string? Codigo { get; set; }

        public DateTime? FechaAsignacion { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // "Asignado" | "Actualizado" | "Removido"
        [StringLength(50)]
        public string? Accion { get; set; }

        [StringLength(200)]
        public string? UsuarioNombre { get; set; }

        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;
    }
}
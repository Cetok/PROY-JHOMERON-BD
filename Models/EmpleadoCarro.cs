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

        // ── Licencia de conducir ─────────────────────────────────
        public bool TieneLicencia { get; set; } = false;

        [StringLength(50)]
        public string? ClaseLicencia { get; set; }           // IA, IIA, IIB, IIIA, etc.

        public DateOnly? LicenciaEmision { get; set; }
        public DateOnly? LicenciaExpiracion { get; set; }

        // ── Licencia especial ────────────────────────────────────
        public bool TieneLicenciaEspecial { get; set; } = false;

        [StringLength(50)]
        public string? ClaseLicenciaEspecial { get; set; }

        public DateOnly? LicenciaEspecialEmision { get; set; }
        public DateOnly? LicenciaEspecialExpiracion { get; set; }

        // ── Navegación ───────────────────────────────────────────
        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;

        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;
    }
}
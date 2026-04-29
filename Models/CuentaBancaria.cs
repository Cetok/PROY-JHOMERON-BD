using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("CuentasBancarias")]
    public class CuentaBancaria
    {
        [Key]
        public int IdCuenta { get; set; }

        [Required]
        public int IdEmpleado { get; set; }

        /// <summary>BCP, Interbank, BBVA, Scotiabank, BanBif, Pichincha, Nación, etc.</summary>
        [Required, StringLength(80)]
        public string TipoBanco { get; set; } = string.Empty;

        /// <summary>Ahorro, Corriente, CTS, etc.</summary>
        [StringLength(60)]
        public string? TipoCuenta { get; set; }

        [Required, StringLength(30)]
        public string NumeroCuenta { get; set; } = string.Empty;

        /// <summary>CCI interbancario (opcional)</summary>
        [StringLength(30)]
        public string? NumeroCCI { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;
    }
}
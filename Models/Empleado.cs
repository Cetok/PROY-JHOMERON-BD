using System.ComponentModel.DataAnnotations;
namespace PROYJHOME2026.Models
{
    public class Empleado
    {
        [Key]
        public int idEmpleado { get; set; }

        /// <summary>"DNI" o "Carnet"</summary>
        [StringLength(10)]
        public string? TipoDocumento { get; set; } = "DNI";

        public string? dni { get; set; }
        public string? nombre { get; set; }
        public string? paterno { get; set; }
        public string? materno { get; set; }
        public string? correo { get; set; }
        public string? direccion { get; set; }
        public string? estado { get; set; }
    }
}
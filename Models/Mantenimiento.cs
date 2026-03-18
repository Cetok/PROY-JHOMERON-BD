using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("TipoMantenimiento")]
    public class TipoMantenimiento
    {
        [Key]
        public int IdTipoMante { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public ICollection<MantenimientoCarro> MantenimientosCarros { get; set; } = new List<MantenimientoCarro>();
    }

    // ── Mantenimiento de Carro ───────────────────────────────
    [Table("Mantenimiento_carro")]
    public class MantenimientoCarro
    {
        [Key]
        public int IdMante { get; set; }

        [Required]
        public int IdTipoMante { get; set; }

        [Required]
        public int IdCarro { get; set; }

        // Quién registró el mantenimiento
        public int? IdUsuarioCreador { get; set; }

        // Fecha en que se registró en el sistema
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Fecha programada para realizar el mantenimiento
        [Required]
        public DateTime FechaProgramada { get; set; }

        // Fecha en que se inició (cambio a En proceso)
        public DateTime? FechaInicio { get; set; }

        // Fecha en que se culminó
        public DateTime? FechaCulminada { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Estados: Pendiente | En proceso | Culminado | Cancelado
        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente";

        // Navegación
        [ForeignKey("IdTipoMante")]
        public TipoMantenimiento TipoMantenimiento { get; set; } = null!;

        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;

        [ForeignKey("IdUsuarioCreador")]
        public Usuario? UsuarioCreador { get; set; }
    }

    // ── Notificaciones ───────────────────────────────────────
    [Table("Notificaciones")]
    public class Notificacion
    {
        [Key]
        public int IdNotificacion { get; set; }

        // A qué usuario va dirigida
        [Required]
        public int IdUsuario { get; set; }

        // Tipo: Mantenimiento | Creacion | Edicion | Eliminacion | Sistema
        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Mensaje { get; set; }

        // Link opcional para ir directamente al registro
        [StringLength(200)]
        public string? Url { get; set; }

        public bool Leida { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Referencia opcional al mantenimiento
        public int? IdMante { get; set; }

        // Navegación
        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; } = null!;
    }

    // ── Auditoría ────────────────────────────────────────────
    [Table("AuditoriaLog")]
    public class AuditoriaLog
    {
        [Key]
        public int IdLog { get; set; }

        public int? IdUsuario { get; set; }

        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        // Acción: Crear | Editar | Eliminar | Login | Logout | CambioEstado
        [Required]
        [StringLength(50)]
        public string Accion { get; set; } = string.Empty;

        // Tabla/entidad afectada: Carro, Empleado, Mantenimiento, etc.
        [Required]
        [StringLength(100)]
        public string Entidad { get; set; } = string.Empty;

        // ID del registro afectado
        public int? IdEntidad { get; set; }

        // Descripción legible: "Registró vehículo ABC-123"
        [StringLength(500)]
        public string? Descripcion { get; set; }

        // Datos anteriores (JSON) — para ediciones
        public string? DatosAnteriores { get; set; }

        // Fecha y hora exacta
        public DateTime FechaHora { get; set; } = DateTime.Now;

        // IP del cliente
        [StringLength(50)]
        public string? IpCliente { get; set; }
    }
}
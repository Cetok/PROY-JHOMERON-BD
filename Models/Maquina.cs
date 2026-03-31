using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    // ── Máquina ───────────────────────────────────────────────
    [Table("Maquinas")]
    public class Maquina
    {
        [Key]
        public int IdMaquina { get; set; }

        [Required]
        [StringLength(50)]
        public string NumeroMaquina { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string NombreMaquina { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Marca { get; set; }

        /// <summary>Fecha de adquisición o puesta en servicio.</summary>
        public DateTime? FechaAdquisicion { get; set; }

        /// <summary>Accesorios/partes — texto libre detallando piezas.</summary>
        [StringLength(2000)]
        public string? AccesoriosParte { get; set; }

        public DateTime? FechaCompra { get; set; }

        /// <summary>Activo | Mantenimiento | Inoperativo</summary>
        [Required]
        [StringLength(100)]
        public string Estado { get; set; } = "Activo";

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public int? IdUsuarioCreador { get; set; }

        // Navegación
        public MaquinaAsignacion? AsignacionActual { get; set; }
        public ICollection<MaquinaLog> Logs { get; set; } = new List<MaquinaLog>();
    }

    // ── Asignación de Máquina ─────────────────────────────────
    [Table("MaquinaAsignaciones")]
    public class MaquinaAsignacion
    {
        [Key]
        public int IdAsignacion { get; set; }

        [Required]
        public int IdMaquina { get; set; }

        [Required]
        public int IdGrupo { get; set; }

        /// <summary>Empleado encargado de la máquina en ese grupo.</summary>
        [Required]
        public int IdEmpleadoEncargado { get; set; }

        [Required]
        public DateTime FechaAsignacion { get; set; }

        /// <summary>Operativo | Inactivo</summary>
        [Required]
        [StringLength(50)]
        public string EstadoOperativo { get; set; } = "Operativo";

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        public bool EsActiva { get; set; } = true;

        // Navegación
        [ForeignKey("IdMaquina")]
        public Maquina Maquina { get; set; } = null!;

        [ForeignKey("IdGrupo")]
        public Grupo Grupo { get; set; } = null!;

        [ForeignKey("IdEmpleadoEncargado")]
        public Empleado Encargado { get; set; } = null!;
    }

    // ── Log / Historial de Máquina ────────────────────────────
    [Table("MaquinaLogs")]
    public class MaquinaLog
    {
        [Key]
        public int IdLog { get; set; }

        [Required]
        public int IdMaquina { get; set; }

        public int? IdUsuario { get; set; }

        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        /// <summary>CambioEstado | CambioAsignacion | CambioEncargado | Edicion</summary>
        [Required]
        [StringLength(50)]
        public string TipoEvento { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ValorAnterior { get; set; }

        [StringLength(200)]
        public string? ValorNuevo { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [ForeignKey("IdMaquina")]
        public Maquina Maquina { get; set; } = null!;
    }
}
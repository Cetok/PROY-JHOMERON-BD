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

        /// <summary>Accesorios/partes — texto libre detallando piezas.</summary>
        [StringLength(2000)]
        public string? AccesoriosParte { get; set; }

        public DateTime? FechaCompra { get; set; }

        /// <summary>Registrado | Activo | Mantenimiento | Inoperativo</summary>
        [Required]
        [StringLength(100)]
        public string Estado { get; set; } = "Registrado";

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public int? IdUsuarioCreador { get; set; }

        // Navegación
        public ICollection<MaquinaAsignacion> Asignaciones { get; set; } = new List<MaquinaAsignacion>();
        public ICollection<MaquinaLog> Logs { get; set; } = new List<MaquinaLog>();

        // Helper: devuelve la asignación activa actual (puede ser null)
        [NotMapped]
        public MaquinaAsignacion? AsignacionActual => Asignaciones.FirstOrDefault(a => a.EsActiva);
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

        /// <summary>Empleado encargado principal (se mantiene por compatibilidad, puede ser null si hay múltiples).</summary>
        public int? IdEmpleadoEncargado { get; set; }

        public DateTime? FechaAsignacion { get; set; }

        /// <summary>Operativo | Inactivo</summary>
        [Required]
        [StringLength(50)]
        public string EstadoOperativo { get; set; } = "Operativo";

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        /// <summary>Área específica donde opera la máquina. Ej: Área de Mezcla, Llenado, etc.</summary>
        [StringLength(200)]
        public string? AreaEspecifica { get; set; }

        public bool EsActiva { get; set; } = true;

        // Navegación
        [ForeignKey("IdMaquina")]
        public Maquina Maquina { get; set; } = null!;

        [ForeignKey("IdGrupo")]
        public Grupo Grupo { get; set; } = null!;

        [ForeignKey("IdEmpleadoEncargado")]
        public Empleado? Encargado { get; set; }

        /// <summary>Lista de encargados (máx. 5) asignados a esta máquina.</summary>
        public ICollection<MaquinaAsignacionEncargado> Encargados { get; set; } = new List<MaquinaAsignacionEncargado>();
    }

    // ── Encargados de una Asignación (máx. 5) ────────────────
    [Table("MaquinaAsignacionEncargados")]
    public class MaquinaAsignacionEncargado
    {
        [Key]
        public int IdEncargado { get; set; }

        [Required]
        public int IdAsignacion { get; set; }

        [Required]
        public int IdEmpleado { get; set; }

        public DateTime FechaAgregado { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("IdAsignacion")]
        public MaquinaAsignacion Asignacion { get; set; } = null!;

        [ForeignKey("IdEmpleado")]
        public Empleado Empleado { get; set; } = null!;
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
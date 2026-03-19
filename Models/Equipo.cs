using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    [Table("Equipos")]
    public class Equipo
    {
        [Key]
        public int idEquipo { get; set; }

        public int idTipoEquipo { get; set; }
        public TipoEquipo? TipoEquipo { get; set; }

        [StringLength(100)]
        public string? marca { get; set; }

        [StringLength(100)]
        public string? modelo { get; set; }

        [StringLength(100)]
        public string? sistema_operativo { get; set; }

        [StringLength(100)]
        public string? version { get; set; }

        [StringLength(100)]
        public string? numero_serie { get; set; }

        public string estado_equipo { get; set; } = "Activo";

        public DateTime fecha_compra { get; set; }

        // ── Observaciones (todos los equipos) ────────────────────
        [StringLength(1000)]
        public string? Observaciones { get; set; }

        // ── Campos técnicos (solo CPU y Laptop) ──────────────────
        [StringLength(200)]
        public string? Procesador { get; set; }

        [StringLength(200)]
        public string? TarjetaMadre { get; set; }

        [StringLength(100)]
        public string? Ram { get; set; }

        [StringLength(200)]
        public string? Disco { get; set; }

        // Solo CPU — fuente de energía
        [StringLength(200)]
        public string? FuenteEnergia { get; set; }

        // null = no aplica, true = integrados, false = tarjeta dedicada
        public bool? GraficosIntegrados { get; set; }

        [StringLength(200)]
        public string? TarjetaGrafica { get; set; }

        // Navegación
        public ICollection<Asignacion> Asignaciones { get; set; } = new List<Asignacion>();
        public ICollection<EquipoComponenteLog> ComponenteLogs { get; set; } = new List<EquipoComponenteLog>();
    }

    // ── Historial de cambios de componentes ──────────────────────
    [Table("EquipoComponenteLogs")]
    public class EquipoComponenteLog
    {
        [Key]
        public int IdLog { get; set; }

        [Required]
        public int IdEquipo { get; set; }

        public int? IdUsuario { get; set; }

        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        // Tipo: CambioComponente | Mantenimiento | ActualizacionSO
        [Required]
        [StringLength(50)]
        public string TipoEvento { get; set; } = string.Empty;

        // Componente afectado: Procesador, RAM, Disco, SO, etc.
        [StringLength(100)]
        public string? Componente { get; set; }

        [StringLength(300)]
        public string? ValorAnterior { get; set; }

        [StringLength(300)]
        public string? ValorNuevo { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("IdEquipo")]
        public Equipo Equipo { get; set; } = null!;
    }
}
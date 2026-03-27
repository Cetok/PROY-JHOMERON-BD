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

        // ── Observaciones generales (todos los equipos) ──────────
        [StringLength(1000)]
        public string? Observaciones { get; set; }

        // ── Campos técnicos (CPU y Laptop) ───────────────────────
        [StringLength(200)]
        public string? Procesador { get; set; }

        [StringLength(200)]
        public string? TarjetaMadre { get; set; }

        [StringLength(100)]
        public string? Ram { get; set; }

        [StringLength(200)]
        public string? Disco { get; set; }

        [StringLength(200)]
        public string? FuenteEnergia { get; set; }

        public bool? GraficosIntegrados { get; set; }

        [StringLength(200)]
        public string? TarjetaGrafica { get; set; }

        // ── Campos Celular ────────────────────────────────────────
        [StringLength(50)]
        public string? IMEI { get; set; }

        // ── Campos Mouse ──────────────────────────────────────────
        public bool? EsInalambrico { get; set; }

        // ── Campos PC Completo ────────────────────────────────────

        // CPU
        [StringLength(100)]
        public string? PcCpuMarca { get; set; }
        [StringLength(100)]
        public string? PcCpuModelo { get; set; }
        [StringLength(100)]
        public string? PcCpuSerie { get; set; }
        [StringLength(200)]
        public string? PcCpuProcesador { get; set; }
        [StringLength(200)]
        public string? PcCpuTarjetaMadre { get; set; }
        [StringLength(100)]
        public string? PcCpuRam { get; set; }
        [StringLength(200)]
        public string? PcCpuDisco { get; set; }
        [StringLength(200)]
        public string? PcCpuFuenteEnergia { get; set; }
        public bool? PcCpuGraficosIntegrados { get; set; }
        [StringLength(200)]
        public string? PcCpuTarjetaGrafica { get; set; }
        [StringLength(100)]
        public string? PcCpuSistemaOperativo { get; set; }
        [StringLength(100)]
        public string? PcCpuVersionSO { get; set; }

        // Monitor
        [StringLength(100)]
        public string? PcMonitorMarca { get; set; }
        [StringLength(100)]
        public string? PcMonitorModelo { get; set; }
        [StringLength(100)]
        public string? PcMonitorSerie { get; set; }

        // Mouse
        [StringLength(100)]
        public string? PcMouseMarca { get; set; }
        [StringLength(100)]
        public string? PcMouseModelo { get; set; }
        [StringLength(100)]
        public string? PcMouseSerie { get; set; }
        public bool? PcMouseEsInalambrico { get; set; }

        // Teclado
        [StringLength(100)]
        public string? PcTecladoMarca { get; set; }
        [StringLength(100)]
        public string? PcTecladoModelo { get; set; }
        [StringLength(100)]
        public string? PcTecladoSerie { get; set; }

        // Mousepad
        [StringLength(100)]
        public string? PcMousepadMarca { get; set; }

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

        [Required]
        [StringLength(50)]
        public string TipoEvento { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Componente { get; set; }

        [StringLength(300)]
        public string? ValorAnterior { get; set; }

        [StringLength(300)]
        public string? ValorNuevo { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [ForeignKey("IdEquipo")]
        public Equipo Equipo { get; set; } = null!;
    }
}
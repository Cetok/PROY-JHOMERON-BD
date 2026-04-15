using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    /// <summary>Encabezado de la Inspección de Botiquín por Grupo (Planta).</summary>
    [Table("InspeccionBotiquinGrupo")]
    public class InspeccionBotiquinGrupo
    {
        [Key]
        public int IdInspeccion { get; set; }

        [Required]
        public int IdGrupo { get; set; }

        [Required]
        public DateOnly FechaInspeccion { get; set; }

        /// <summary>Piso — texto libre.</summary>
        [StringLength(50)]
        public string? Piso { get; set; }

        /// <summary>N° de Botiquín — texto libre (ej: BOT-01).</summary>
        [Required, StringLength(50)]
        public string NumeroBotiquin { get; set; } = string.Empty;

        /// <summary>Área del grupo — se toma del nombre del grupo automáticamente.</summary>
        [StringLength(100)]
        public string Area { get; set; } = string.Empty;

        // ── 4 checks del encabezado ──────────────────────────────
        /// <summary>El botiquín se encuentra instalado en la pared.</summary>
        public bool InstaladoEnPared   { get; set; }
        public bool LocalizadoVisible  { get; set; }
        public bool LibreDeObstaculos  { get; set; }
        public bool Senalizado         { get; set; }

        // ── Inspección realizada por ─────────────────────────────
        [Required, StringLength(200)]
        public string InspeccionadoPor { get; set; } = string.Empty;

        [Required]
        public string FirmaBase64 { get; set; } = string.Empty;

        // ── Auditoría ────────────────────────────────────────────
        public int? IdUsuario { get; set; }

        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>True si ya fue editado una vez.</summary>
        public bool FueEditado { get; set; } = false;

        // ── Navegación ───────────────────────────────────────────
        [ForeignKey("IdGrupo")]
        public Grupo Grupo { get; set; } = null!;

        public ICollection<InspeccionBotiquinGrupoItem> Items { get; set; }
            = new List<InspeccionBotiquinGrupoItem>();
    }

    /// <summary>Cada elemento del botiquín inspeccionado por grupo.</summary>
    [Table("InspeccionBotiquinGrupoItem")]
    public class InspeccionBotiquinGrupoItem
    {
        [Key]
        public int IdItem { get; set; }

        [Required]
        public int IdInspeccion { get; set; }

        [Required, StringLength(300)]
        public string Elemento { get; set; } = string.Empty;

        public bool SeEncuentra { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public DateOnly FechaVencimiento { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [ForeignKey("IdInspeccion")]
        public InspeccionBotiquinGrupo Inspeccion { get; set; } = null!;
    }
}
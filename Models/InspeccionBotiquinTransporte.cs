using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    /// <summary>Encabezado de la Inspección de Botiquín — Transporte.</summary>
    [Table("InspeccionBotiquinTransporte")]
    public class InspeccionBotiquinTransporte
    {
        [Key]
        public int IdInspeccion { get; set; }

        [Required]
        public int IdCarro { get; set; }

        [Required]
        public DateOnly FechaInspeccion { get; set; }

        [Required, StringLength(50)]
        public string NumeroBotiquin { get; set; } = string.Empty;

        // ── 4 checks del encabezado ──────────────────────────────
        public bool UbicadoEnSuLugar    { get; set; }
        public bool LocalizadoVisible   { get; set; }
        public bool LibreDeObstaculos   { get; set; }
        public bool Senalizado          { get; set; }

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
        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;

        public ICollection<InspeccionBotiquinTransporteItem> Items { get; set; }
            = new List<InspeccionBotiquinTransporteItem>();
    }

    /// <summary>Cada elemento del botiquín inspeccionado.</summary>
    [Table("InspeccionBotiquinTransporteItem")]
    public class InspeccionBotiquinTransporteItem
    {
        [Key]
        public int IdItem { get; set; }

        [Required]
        public int IdInspeccion { get; set; }

        [Required, StringLength(300)]
        public string Elemento { get; set; } = string.Empty;

        /// <summary>true = SI, false = NO.</summary>
        public bool SeEncuentra { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public DateOnly FechaVencimiento { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // ── Navegación ───────────────────────────────────────────
        [ForeignKey("IdInspeccion")]
        public InspeccionBotiquinTransporte Inspeccion { get; set; } = null!;
    }
}
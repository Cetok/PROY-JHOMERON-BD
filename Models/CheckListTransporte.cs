using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    /// <summary>Encabezado del Check List de Inspección de Transporte.</summary>
    [Table("CheckListTransporte")]
    public class CheckListTransporte
    {
        [Key]
        public int IdCheckList { get; set; }

        [Required]
        public int IdCarro { get; set; }

        /// <summary>Fecha de inspección (solo date, siempre = hoy al crear).</summary>
        [Required]
        public DateOnly FechaInspeccion { get; set; }

        /// <summary>Hora automática al abrir el formulario.</summary>
        [Required]
        public TimeOnly HoraInspeccion { get; set; }

        /// <summary>Sede / Área — fijo "Transporte".</summary>
        [StringLength(100)]
        public string SedeArea { get; set; } = "Transporte";

        /// <summary>Nombre del responsable que firma.</summary>
        [Required, StringLength(200)]
        public string NombreResponsable { get; set; } = string.Empty;

        /// <summary>Firma digital en base64 (PNG del canvas).</summary>
        [Required]
        public string FirmaBase64 { get; set; } = string.Empty;

        /// <summary>Observaciones generales del checklist.</summary>
        [StringLength(1000)]
        public string? ObservacionesGenerales { get; set; }

        /// <summary>Usuario del sistema que registró.</summary>
        public int? IdUsuario { get; set; }

        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // ── Navegación ──────────────────────────────────────────
        [ForeignKey("IdCarro")]
        public Carro Carro { get; set; } = null!;

        public ICollection<CheckListTransporteItem> Items { get; set; } = new List<CheckListTransporteItem>();
    }

    /// <summary>Cada ítem (fila) del check list con su respuesta SI/NO y observación.</summary>
    [Table("CheckListTransporteItem")]
    public class CheckListTransporteItem
    {
        [Key]
        public int IdItem { get; set; }

        [Required]
        public int IdCheckList { get; set; }

        /// <summary>Número de sección (1-6).</summary>
        public int Seccion { get; set; }

        /// <summary>Nombre de la sección (ej. "VISTA DEL EXTERIOR EQUIPO").</summary>
        [StringLength(200)]
        public string NombreSeccion { get; set; } = string.Empty;

        /// <summary>Texto del elemento a inspeccionar.</summary>
        [Required, StringLength(500)]
        public string Elemento { get; set; } = string.Empty;

        /// <summary>true = SI, false = NO, null = NA.</summary>
        public bool? Cumple { get; set; }

        [StringLength(500)]
        public string? Observacion { get; set; }

        // ── Navegación ──────────────────────────────────────────
        [ForeignKey("IdCheckList")]
        public CheckListTransporte CheckList { get; set; } = null!;
    }
}
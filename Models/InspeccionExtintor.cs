using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROYJHOME2026.Models
{
    /// <summary>Encabezado de la Inspección de Extintores.</summary>
    [Table("InspeccionExtintor")]
    public class InspeccionExtintor
    {
        [Key]
        public int IdInspeccion { get; set; }

        /// <summary>IdAsesorio del extintor (siempre será un accesorio de tipo Extintor).</summary>
        [Required]
        public int IdAsesorio { get; set; }

        [Required]
        public DateOnly FechaInspeccion { get; set; }

        [Required, StringLength(200)]
        public string InspeccionadoPor { get; set; } = string.Empty;

        [Required]
        public string FirmaBase64 { get; set; } = string.Empty;

        public int?   IdUsuario     { get; set; }
        [StringLength(100)]
        public string? NombreUsuario { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>True si ya fue editado una vez.</summary>
        public bool FueEditado { get; set; } = false;

        // ── Navegación ───────────────────────────────────────────
        [ForeignKey("IdAsesorio")]
        public Asesorio Asesorio { get; set; } = null!;

        public ICollection<InspeccionExtintorFila> Filas { get; set; }
            = new List<InspeccionExtintorFila>();
    }

    /// <summary>
    /// Una fila por cada grupo que tiene asignado ese extintor.
    /// Contiene tipo/peso/vencimiento jalados del GrupoAsesorio,
    /// el comentario del área y las observaciones marcadas (1-18).
    /// </summary>
    [Table("InspeccionExtintorFila")]
    public class InspeccionExtintorFila
    {
        [Key]
        public int IdFila { get; set; }

        [Required]
        public int IdInspeccion { get; set; }

        [Required]
        public int IdGrupo { get; set; }

        /// <summary>Nombre del área/grupo — se copia al momento de registrar.</summary>
        [StringLength(100)]
        public string NombreGrupo { get; set; } = string.Empty;

        /// <summary>Tipo de extintor jalado de GrupoAsesorio.</summary>
        [StringLength(10)]
        public string? TipoExtintor { get; set; }

        /// <summary>Peso jalado de GrupoAsesorio.</summary>
        [StringLength(50)]
        public string? PesoExtintor { get; set; }

        /// <summary>Fecha de vencimiento jalada de GrupoAsesorio.</summary>
        public DateOnly? FechaVencimiento { get; set; }

        /// <summary>
        /// Comentario del área: indica que este extintor abarca otras áreas.
        /// </summary>
        [StringLength(500)]
        public string? Comentario { get; set; }

        /// <summary>
        /// Observaciones marcadas del 1 al 17 guardadas como CSV (ej: "1,3,7").
        /// Si está vacío = todo OK.
        /// </summary>
        [StringLength(100)]
        public string? ObservacionesMarcadas { get; set; }

        /// <summary>Texto libre del ítem 18 "Otros (Indicar)". Obligatorio si 18 está marcado.</summary>
        [StringLength(500)]
        public string? Observacion18 { get; set; }

        // ── Navegación ───────────────────────────────────────────
        [ForeignKey("IdInspeccion")]
        public InspeccionExtintor Inspeccion { get; set; } = null!;

        [ForeignKey("IdGrupo")]
        public Grupo Grupo { get; set; } = null!;
    }
}
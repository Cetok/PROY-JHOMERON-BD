using System.ComponentModel.DataAnnotations;
namespace PROYJHOME2026.Models
{
    public class Grupo
    {
        [Key]
        public int idGrupo {set;get;}
        public string? area {set;get;}
    }
}
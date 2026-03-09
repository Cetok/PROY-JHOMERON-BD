using System.ComponentModel.DataAnnotations;
namespace PROYJHOME2026.Models
{
    public class Empleado
    {
        [Key]
        public int idEmpleado {get;set;}
        public String? dni {get;set;}
        public String? nombre {get;set;}
        public String? paterno {get;set;}
        public String? materno {get;set;}
        public String? correo {get;set;}
        public String? direccion {get;set;}
        public String? estado {get;set;}

    }
}
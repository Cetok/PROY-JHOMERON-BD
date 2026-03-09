using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Empleado> Empleados { get; set; }

        public DbSet<Equipo> Equipos { get; set; }

        public DbSet<TipoEquipo> TipoEquipos {get;set;}
    }
}
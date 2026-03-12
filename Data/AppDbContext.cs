using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── Tablas principales ──────────────────────────────────
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<TipoEquipo> TiposEquipo { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Motivo> Motivos { get; set; }
        public DbSet<Historial> Historiales { get; set; }
        public DbSet<Chip> Chips { get; set; }
        public DbSet<Asignacion> Asignaciones { get; set; }
        public DbSet<Carro> Carros { get; set; }
        public DbSet<Seguro> Seguros { get; set; }
        public DbSet<TipoMantenimiento> TiposMantenimiento { get; set; }
        public DbSet<MantenimientoCarro> MantenimientosCarros { get; set; }
        public DbSet<Asesorio> Asesorios { get; set; }
        public DbSet<Modalidad> Modalidades { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        // ── Tablas de relación (many-to-many) ───────────────────
        public DbSet<EmpleadoGrupo> EmpleadoGrupos { get; set; }
        public DbSet<EmpleadoCarro> EmpleadosCarros { get; set; }
        public DbSet<EmpleadoSeguro> EmpleadoSeguros { get; set; }
        public DbSet<CarroSeguro> CarroSeguros { get; set; }
        public DbSet<CarroAsesorio> CarroAsesorios { get; set; }
        public DbSet<GrupoAsesorio> GrupoAsesorios { get; set; }
        public DbSet<CarroModalidad> CarroModalidades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Claves compuestas (tablas de relación) ───────────

            modelBuilder.Entity<EmpleadoGrupo>()
                .HasKey(eg => new { eg.IdEmpleado, eg.IdGrupo });

            modelBuilder.Entity<EmpleadoCarro>()
                .HasKey(ec => new { ec.IdEmpleado, ec.IdCarro });

            modelBuilder.Entity<EmpleadoSeguro>()
                .HasKey(es => new { es.IdEmpleado, es.IdSeguro });

            modelBuilder.Entity<CarroSeguro>()
                .HasKey(cs => new { cs.IdCarro, cs.IdSeguro });

            modelBuilder.Entity<CarroAsesorio>()
                .HasKey(ca => new { ca.IdCarro, ca.IdAsesorio });

            modelBuilder.Entity<GrupoAsesorio>()
                .HasKey(ga => new { ga.IdGrupo, ga.IdAsesorio });

            modelBuilder.Entity<CarroModalidad>()
                .HasKey(cm => new { cm.IdCarro, cm.IdModalidad });

            // ── Relaciones explícitas ────────────────────────────

            // Historial → Asignacion
            modelBuilder.Entity<Historial>()
                .HasOne(h => h.Asignacion)
                .WithMany(a => a.Historiales)
                .HasForeignKey(h => h.IdAsignacion)
                .OnDelete(DeleteBehavior.Restrict);

            // Historial → Motivo
            modelBuilder.Entity<Historial>()
                .HasOne(h => h.Motivo)
                .WithMany(m => m.Historiales)
                .HasForeignKey(h => h.IdMotivo)
                .OnDelete(DeleteBehavior.Restrict);

            // Asignacion → Empleado
            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Empleado)
                .WithMany()
                .HasForeignKey(a => a.IdEmpleado)
                .OnDelete(DeleteBehavior.Restrict);

            // Equipo → TipoEquipo
            modelBuilder.Entity<Equipo>()
                .HasOne(e => e.TipoEquipo)
                .WithMany()
                .HasForeignKey(e => e.idTipoEquipo)
                .OnDelete(DeleteBehavior.Restrict);

            // Asignacion → Equipo (corregido)
            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Equipo)
                .WithMany(e => e.Asignaciones)  // ← apunta a la colección
                .HasForeignKey(a => a.IdEquipo)
                .OnDelete(DeleteBehavior.Restrict);

            // Asignacion → Chip (opcional)
            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Chip)
                .WithMany(c => c.Asignaciones)
                .HasForeignKey(a => a.IdChip)
                .OnDelete(DeleteBehavior.SetNull);

            // MantenimientoCarro → TipoMantenimiento
            modelBuilder.Entity<MantenimientoCarro>()
                .HasOne(m => m.TipoMantenimiento)
                .WithMany(t => t.MantenimientosCarros)
                .HasForeignKey(m => m.IdTipoMante)
                .OnDelete(DeleteBehavior.Restrict);

            // MantenimientoCarro → Carro
            modelBuilder.Entity<MantenimientoCarro>()
                .HasOne(m => m.Carro)
                .WithMany(c => c.MantenimientosCarros)
                .HasForeignKey(m => m.IdCarro)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.username)
                .IsUnique();    
        }
    }
}
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── Tablas principales ──────────────────────────────────
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<CuentaBancaria> CuentasBancarias { get; set; }
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
        public DbSet<Maquina> Maquinas { get; set; }
        public DbSet<MaquinaAsignacion> MaquinaAsignaciones { get; set; }
        public DbSet<MaquinaAsignacionEncargado> MaquinaAsignacionEncargados { get; set; }
        public DbSet<MaquinaLog> MaquinaLogs { get; set; }
        public DbSet<MaquinaAccesorioCambio> MaquinaAccesorioCambios { get; set; }
         public DbSet<IAConversacion> IAConversaciones { get; set; }
        public DbSet<IAMensaje>      IAMensajes       { get; set; }

        // ── Nuevas tablas ───────────────────────────────────────
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<AuditoriaLog> AuditoriaLogs { get; set; }
        public DbSet<CarroEstadoLog> CarroEstadoLogs { get; set; }
        public DbSet<EquipoComponenteLog> EquipoComponenteLogs { get; set; }
        public DbSet<CarroConductorLog> CarroConductorLogs { get; set; }
        public DbSet<EmpleadoEstadoLog> EmpleadoEstadoLogs { get; set; }
        public DbSet<CheckListTransporte>     CheckListTransportes      { get; set; }
        public DbSet<CheckListTransporteItem> CheckListTransporteItems  { get; set; }
        public DbSet<InspeccionBotiquinTransporte>     InspeccionBotiquinTransportes     { get; set; }
        public DbSet<InspeccionBotiquinTransporteItem> InspeccionBotiquinTransporteItems { get; set; }
        public DbSet<InspeccionBotiquinGrupo>     InspeccionBotiquinGrupos     { get; set; }
        public DbSet<InspeccionBotiquinGrupoItem> InspeccionBotiquinGrupoItems { get; set; }
        public DbSet<InspeccionExtintor>      InspeccionExtintores      { get; set; }
        public DbSet<InspeccionExtintorFila>  InspeccionExtintorFilas   { get; set; }
        public DbSet<CarroModalidadLog>       CarroModalidadLogs        { get; set; }
        public DbSet<HabilitacionVehicular>   HabilitacionesVehiculares { get; set; }
        public DbSet<LunaPolarizada>          LunasPolarizadas          { get; set; }

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

            // ── Claves compuestas ────────────────────────────────
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

            // ── Relaciones ───────────────────────────────────────
            modelBuilder.Entity<Historial>()
                .HasOne(h => h.Asignacion).WithMany(a => a.Historiales)
                .HasForeignKey(h => h.IdAsignacion).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Historial>()
                .HasOne(h => h.Motivo).WithMany(m => m.Historiales)
                .HasForeignKey(h => h.IdMotivo).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Empleado).WithMany()
                .HasForeignKey(a => a.IdEmpleado).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipo>()
                .HasOne(e => e.TipoEquipo).WithMany()
                .HasForeignKey(e => e.idTipoEquipo).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Equipo).WithMany(e => e.Asignaciones)
                .HasForeignKey(a => a.IdEquipo).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Chip).WithMany(c => c.Asignaciones)
                .HasForeignKey(a => a.IdChip).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Grupo).WithMany()
                .HasForeignKey(a => a.IdGrupo).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MantenimientoCarro>()
                .HasOne(m => m.TipoMantenimiento).WithMany(t => t.MantenimientosCarros)
                .HasForeignKey(m => m.IdTipoMante).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MantenimientoCarro>()
                .HasOne(m => m.Carro).WithMany(c => c.MantenimientosCarros)
                .HasForeignKey(m => m.IdCarro).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MantenimientoCarro>()
                .HasOne(m => m.UsuarioCreador).WithMany()
                .HasForeignKey(m => m.IdUsuarioCreador).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notificacion>()
                .HasOne(n => n.Usuario).WithMany()
                .HasForeignKey(n => n.IdUsuario).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarroEstadoLog>()
                .HasOne(l => l.Carro).WithMany()
                .HasForeignKey(l => l.IdCarro).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmpleadoEstadoLog>()
                .HasOne(l => l.Empleado).WithMany()
                .HasForeignKey(l => l.IdEmpleado).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarroConductorLog>()
                .HasOne(l => l.Carro).WithMany()
                .HasForeignKey(l => l.IdCarro).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarroModalidadLog>()
                .HasOne(l => l.Carro).WithMany()
                .HasForeignKey(l => l.IdCarro).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.username).IsUnique();

            modelBuilder.Entity<CheckListTransporte>()
                .HasOne(cl => cl.Carro).WithMany()
                .HasForeignKey(cl => cl.IdCarro).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CheckListTransporteItem>()
                .HasOne(i => i.CheckList).WithMany(cl => cl.Items)
                .HasForeignKey(i => i.IdCheckList).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InspeccionBotiquinTransporte>()
                .HasOne(i => i.Carro).WithMany()
                .HasForeignKey(i => i.IdCarro).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InspeccionBotiquinTransporteItem>()
                .HasOne(i => i.Inspeccion).WithMany(ins => ins.Items)
                .HasForeignKey(i => i.IdInspeccion).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InspeccionBotiquinGrupo>()
                .HasOne(i => i.Grupo).WithMany()
                .HasForeignKey(i => i.IdGrupo).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InspeccionBotiquinGrupoItem>()
                .HasOne(i => i.Inspeccion).WithMany(ins => ins.Items)
                .HasForeignKey(i => i.IdInspeccion).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InspeccionExtintor>()
                .HasOne(i => i.Asesorio).WithMany()
                .HasForeignKey(i => i.IdAsesorio).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InspeccionExtintorFila>()
                .HasOne(f => f.Inspeccion).WithMany(i => i.Filas)
                .HasForeignKey(f => f.IdInspeccion).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InspeccionExtintorFila>()
                .HasOne(f => f.Grupo).WithMany()
                .HasForeignKey(f => f.IdGrupo).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaquinaAsignacion>()
                .HasOne(a => a.Grupo).WithMany()
                .HasForeignKey(a => a.IdGrupo).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaquinaAsignacion>()
                .HasOne(a => a.Encargado).WithMany()
                .HasForeignKey(a => a.IdEmpleadoEncargado).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MaquinaLog>()
                .HasOne(l => l.Maquina).WithMany(m => m.Logs)
                .HasForeignKey(l => l.IdMaquina).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Maquina>()
                .HasMany(m => m.Asignaciones)
                .WithOne(a => a.Maquina)
                .HasForeignKey(a => a.IdMaquina)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaquinaAsignacionEncargado>()
                .HasOne(e => e.Asignacion).WithMany(a => a.Encargados)
                .HasForeignKey(e => e.IdAsignacion).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaquinaAsignacionEncargado>()
                .HasOne(e => e.Empleado).WithMany()
                .HasForeignKey(e => e.IdEmpleado).OnDelete(DeleteBehavior.Restrict);

            // Un empleado no puede estar dos veces en la misma asignación
            modelBuilder.Entity<MaquinaAsignacionEncargado>()
                .HasIndex(e => new { e.IdAsignacion, e.IdEmpleado }).IsUnique();

            modelBuilder.Entity<MaquinaAccesorioCambio>()
                .HasOne(c => c.Maquina).WithMany(m => m.CambiosAccesorios)
                .HasForeignKey(c => c.IdMaquina).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
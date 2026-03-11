using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Seeds
{
    public static class DbSeeder
    {
        /// <summary>
        /// Crea el usuario admin por defecto si no existe.
        /// Llamar desde Program.cs al iniciar la app.
        /// </summary>
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope   = services.CreateScope();
            var context       = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Asegurarse que la BD esté al día
            await context.Database.MigrateAsync();

            // Si ya existe algún usuario no hacer nada
            if (await context.Usuarios.AnyAsync()) return;

            // Crear admin con contraseña hasheada con BCrypt (cost factor 12)
            var admin = new Usuario
            {
                username      = "admin",
                passwordHash  = BCrypt.Net.BCrypt.HashPassword("jhomeron", workFactor: 12),
                rol           = "Admin",
                nombreCompleto = "Administrador del Sistema",
                correo        = "admin@sistema.com",
                activo        = true,
                creadoEn      = DateTime.Now
            };

            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
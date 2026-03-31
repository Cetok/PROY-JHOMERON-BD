using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;

namespace PROYJHOME2026.Seeds
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();

            if (!await context.Usuarios.AnyAsync(u => u.username == "admin"))
                context.Usuarios.Add(new Usuario { username="admin", passwordHash=BCrypt.Net.BCrypt.HashPassword("jhomeron",12), rol="Admin", nombreCompleto="Administrador del Sistema", correo="admin@sistema.com", activo=true, creadoEn=DateTime.Now });

            if (!await context.Usuarios.AnyAsync(u => u.username == "oliver"))
                context.Usuarios.Add(new Usuario { username="oliver", passwordHash=BCrypt.Net.BCrypt.HashPassword("s0p0rt3-6",12), rol="SoporteTI", nombreCompleto="Oliver Amaricua", correo="oliver@sistema.com", activo=true, creadoEn=DateTime.Now });

            if (!await context.Usuarios.AnyAsync(u => u.username == "silvana"))
                context.Usuarios.Add(new Usuario { username="silvana", passwordHash=BCrypt.Net.BCrypt.HashPassword("tr4nsp0-7",12), rol="Transporte", nombreCompleto="Silvana", correo="silvana@sistema.com", activo=true, creadoEn=DateTime.Now });

            if (!await context.Usuarios.AnyAsync(u => u.username == "eusebio"))
                context.Usuarios.Add(new Usuario { username="eusebio", passwordHash=BCrypt.Net.BCrypt.HashPassword("electric0-0",12), rol="Produccion", nombreCompleto="Eusebio", correo="eusebio@sistema.com", activo=true, creadoEn=DateTime.Now });

            await context.SaveChangesAsync();
        }
    }
}
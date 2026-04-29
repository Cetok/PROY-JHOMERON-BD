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

            var usuarios = new (string username, string pass, string rol, string nombre, string correo)[]
            {
                ("admin",   "jhomeron",    "Admin",      "Administrador del Sistema", "admin@sistema.com"),
                ("oliver",  "s0p0rt3-6",  "SoporteTI",  "Oliver Amaricua",           "oliver@sistema.com"),
                ("silvana", "tr4nsp0-7",  "Transporte", "Silvana",                   "silvana@sistema.com"),
                ("eusebio", "electric0-0","Produccion", "Eusebio",                   "eusebio@sistema.com"),
                ("ssoma",   "segu0-0",    "SSOMA",      "SSOMA",                     "ssoma@sistema.com"),
                ("danitza", "sistem4-7",  "Admin",      "Danitza",                   "danitza@sistema.com"),
                ("yane",    "logist1-0",  "Logistica",  "Yane",                      "yanet@sistema.com"),
                ("ayde",    "legal0-0",   "Transporte", "Ayde",                      "ayde@sistema.com"),
            };

            foreach (var (username, pass, rol, nombre, correo) in usuarios)
            {
                if (!await context.Usuarios.AnyAsync(u => u.username == username))
                {
                    context.Usuarios.Add(new Usuario
                    {
                        username       = username,
                        passwordHash   = BCrypt.Net.BCrypt.HashPassword(pass, workFactor: 12),
                        rol            = rol,
                        nombreCompleto = nombre,
                        correo         = correo,
                        activo         = true,
                        creadoEn       = DateTime.Now
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
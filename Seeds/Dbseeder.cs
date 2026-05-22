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
                ("admin",   "jhomeron",    "Admin",      "Administrador del Sistema", "sistemas@jhomeron.com"),
                ("oliver",  "s0p0rt3-6",  "SoporteTI",  "Oliver Amaricua",           "soporte@jhomeron.com"),
                ("silvana", "tr4nsp0-7",  "Transporte", "Silvana",                   "jefeventas@jhomeron.com"),
                ("eusebio", "electric0-0","Produccion", "Eusebio",                   "mantenimientoelectrico@jhomeron.com"),
                ("ssoma",   "segu0-0",    "SSOMA",      "SSOMA",                     "areassoma@jhomeron.com"),
                ("danitza", "sistem4-7",  "Admin",      "Danitza",                   "dllanos@jhomeron.com"),
                ("yane",    "logist1-0",  "Logistica",  "Yane",                      "logistica@jhomeron.com"),
                ("ayde",    "legal0-0",   "Transporte", "Ayde",                      "arealegal@jhomeron.com"),
            };

            foreach (var (username, pass, rol, nombre, correo) in usuarios)
            {
                var usuarioExistente = await context.Usuarios
                    .FirstOrDefaultAsync(u => u.username == username);

                if (usuarioExistente == null)
                {
                    // Crear usuario nuevo
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
                else
                {
                    // Usuario ya existe — solo actualizar el correo
                    usuarioExistente.correo = correo;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
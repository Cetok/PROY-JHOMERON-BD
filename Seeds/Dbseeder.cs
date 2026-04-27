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

            // ── Lista de usuarios del sistema ─────────────────────────────
            // Para cambiar contraseña o correo: edita aquí y reinicia el servidor
            // Se actualiza automáticamente en la BD al arrancar
            var usuarios = new (string username, string pass, string rol, string nombre, string correo)[]
            {
                ("admin",   "jhomeron",      "Admin",      "Administrador del Sistema", "admin@sistema.com"),
                ("oliver",  "s0p0rt3-6",    "SoporteTI",  "Oliver Amaricua",           "oliver@sistema.com"),
                ("silvana", "jhomeron321$",  "Transporte", "Silvana",                   "jefeventas@jhomeron.com"),
                ("eusebio", "electric0-0",  "Produccion", "Eusebio",                   "eusebio@sistema.com"),
                ("ssoma",   "segu0-0",      "SSOMA",      "SSOMA",                     "ssoma@sistema.com"),
                ("danitza", "sistem4-7",    "Admin",      "Danitza",                   "danitza@sistema.com"),
                ("yanet",   "logist1-0",    "Logistica",  "Yanet",                     "yanet@sistema.com"),
                ("ayde",    "legal0-0",     "Transporte", "Ayde",                      "arealegal@jhomeron.com"),
            };

            foreach (var (username, pass, rol, nombre, correo) in usuarios)
            {
                var usuarioExistente = await context.Usuarios
                    .FirstOrDefaultAsync(u => u.username == username);

                if (usuarioExistente == null)
                {
                    // Usuario nuevo — crear
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
                    // Usuario existente — actualizar contraseña si cambió
                    bool passwordCambio = !BCrypt.Net.BCrypt.Verify(pass, usuarioExistente.passwordHash);
                    if (passwordCambio)
                        usuarioExistente.passwordHash = BCrypt.Net.BCrypt.HashPassword(pass, workFactor: 12);

                    // Actualizar correo si cambió
                    if (usuarioExistente.correo != correo)
                        usuarioExistente.correo = correo;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
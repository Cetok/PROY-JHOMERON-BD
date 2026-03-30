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

            // ── Admin ─────────────────────────────────────────────
            if (!await context.Usuarios.AnyAsync(u => u.username == "admin"))
            {
                context.Usuarios.Add(new Usuario
                {
                    username       = "admin",
                    passwordHash   = BCrypt.Net.BCrypt.HashPassword("jhomeron", workFactor: 12),
                    rol            = "Admin",
                    nombreCompleto = "Administrador del Sistema",
                    correo         = "admin@sistema.com",
                    activo         = true,
                    creadoEn       = DateTime.Now
                });
            }

            // ── Oliver — Soporte TI ───────────────────────────────
            if (!await context.Usuarios.AnyAsync(u => u.username == "oliver"))
            {
                context.Usuarios.Add(new Usuario
                {
                    username       = "oliver",
                    passwordHash   = BCrypt.Net.BCrypt.HashPassword("s0p0rt3-6", workFactor: 12),
                    rol            = "SoporteTI",
                    nombreCompleto = "Oliver Amaricua",
                    correo         = "oliver@sistema.com",
                    activo         = true,
                    creadoEn       = DateTime.Now
                });
            }

            // ── Silvana — Transporte ──────────────────────────────
            if (!await context.Usuarios.AnyAsync(u => u.username == "silvana"))
            {
                context.Usuarios.Add(new Usuario
                {
                    username       = "silvana",
                    passwordHash   = BCrypt.Net.BCrypt.HashPassword("tr4nsp0-7", workFactor: 12),
                    rol            = "Transporte",
                    nombreCompleto = "Silvana",
                    correo         = "silvana@sistema.com",
                    activo         = true,
                    creadoEn       = DateTime.Now
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
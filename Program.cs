using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Filters;
using PROYJHOME2026.Seeds;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos ────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Sesión (30 min de inactividad) ──────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly    = true;   // No accesible por JS
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Solo HTTPS
    options.Cookie.SameSite    = SameSiteMode.Strict;
});

// ── MVC con filtro global de autenticación ───────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuthFilter>();
});

// ── Protección anti-CSRF ya incluida con AddControllersWithViews ──

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();   // Debe ir ANTES de MapControllerRoute

// ── Headers de seguridad ─────────────────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"]        = "DENY";
    context.Response.Headers["X-XSS-Protection"]       = "1; mode=block";
    context.Response.Headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");  // Ruta raíz → Login

// ── Seed: crear admin si no existe ──────────────────────────
await DbSeeder.SeedAdminAsync(app.Services);

app.Run();
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.BackgroundServices;
using PROYJHOME2026.Data;
using PROYJHOME2026.Filters;
using PROYJHOME2026.Seeds;
using PROYJHOME2026.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos ────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Sesión (30 min de inactividad) ──────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout         = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly     = true;
    options.Cookie.IsEssential  = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;//cambie esto
    options.Cookie.SameSite     = SameSiteMode.Lax;//cambie esto
});

// ── HttpContextAccessor (necesario para AuditoriaService) ───
builder.Services.AddHttpContextAccessor();

// ── Servicios propios ────────────────────────────────────────
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<EmailService>();

// ── Servicio de background (revisa mantenimientos pendientes)
builder.Services.AddHostedService<MantenimientoBackgroundService>();

// ── MVC con filtro global de autenticación ───────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuthFilter>();
});

var app = builder.Build();
QuestPDF.Settings.License = LicenseType.Community;

// ── Middleware pipeline ──────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

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
    name:    "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");
//agregue lo de abajo
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
                  .GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
//"Server=miapp-db.cdieqi08ih0k.sa-east-1.rds.amazonaws.com,1433;Database=NombreDeTuBD;User Id=admin;Password=TuPassword;TrustServerCertificate=True;"
// ── Seed ─────────────────────────────────────────────────────
await DbSeeder.SeedAdminAsync(app.Services);

app.Run();
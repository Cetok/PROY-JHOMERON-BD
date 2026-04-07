using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Services;
using PROYJHOME2026.Models;
using BCrypt.Net;

namespace PROYJHOME2026.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext    _context;
        private readonly AuditoriaService _auditoriaService;

        // Máximo de intentos antes de bloquear la cuenta
        private const int MaxIntentos      = 5;
        private const int MinutosBloqueo   = 15;

        public AuthController(AppDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        // ── LOGIN GET ────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            // Si ya hay sesión activa → redirigir al dashboard
            if (HttpContext.Session.GetString("UsuarioId") != null)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // ── LOGIN POST ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl)
        {
            // Validación básica vacíos
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error     = "Por favor ingresa tu usuario y contraseña.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // Buscar usuario (case-insensitive)
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.username.ToLower() == username.ToLower());

            // ── Mensaje genérico siempre (no revelar si usuario existe) ──
            const string errorGenerico = "Usuario o contraseña incorrectos.";

            if (usuario == null)
            {
                // Pequeña espera para dificultar timing attacks
                await Task.Delay(300);
                ViewBag.Error     = errorGenerico;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // ── Verificar si está bloqueado ──────────────────────
            if (usuario.bloqueadoHasta.HasValue && usuario.bloqueadoHasta > DateTime.Now)
            {
                var minutosRestantes = (int)Math.Ceiling(
                    (usuario.bloqueadoHasta.Value - DateTime.Now).TotalMinutes);

                ViewBag.Error     = $"Cuenta bloqueada por seguridad. Intenta en {minutosRestantes} minuto(s).";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // ── Verificar cuenta activa ──────────────────────────
            if (!usuario.activo)
            {
                ViewBag.Error     = "Tu cuenta está desactivada. Contacta al administrador.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // ── Verificar contraseña con BCrypt ──────────────────
            bool passwordValido = BCrypt.Net.BCrypt.Verify(password, usuario.passwordHash);

            if (!passwordValido)
            {
                usuario.intentosFallidos++;

                if (usuario.intentosFallidos >= MaxIntentos)
                {
                    usuario.bloqueadoHasta   = DateTime.Now.AddMinutes(MinutosBloqueo);
                    usuario.intentosFallidos = 0;
                    await _context.SaveChangesAsync();

                    ViewBag.Error     = $"Demasiados intentos fallidos. Cuenta bloqueada {MinutosBloqueo} minutos.";
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                await _context.SaveChangesAsync();
                int restantes = MaxIntentos - usuario.intentosFallidos;
                ViewBag.Error     = $"{errorGenerico} Te quedan {restantes} intento(s).";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            // ── Login exitoso ────────────────────────────────────
            usuario.intentosFallidos = 0;
            usuario.bloqueadoHasta   = null;
            usuario.ultimoAcceso     = DateTime.Now;
            await _context.SaveChangesAsync();

            // Guardar datos en sesión
            HttpContext.Session.SetString("UsuarioId",       usuario.idUsuario.ToString());
            HttpContext.Session.SetString("UsuarioNombre",   usuario.nombreCompleto ?? usuario.username);
            HttpContext.Session.SetString("UsuarioUsername", usuario.username);
            HttpContext.Session.SetString("UsuarioRol",      usuario.rol);

            // Auditoría login
            await _auditoriaService.RegistrarAsync("Login", "Usuario", usuario.idUsuario,
                $"Inicio de sesión: {usuario.username}");

            // Redirigir a returnUrl si es válida, si no al dashboard
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
 
            // Redirigir según rol
            return usuario.rol switch
            {
                "SoporteTI"  => RedirectToAction("Dashboard",           "Reportes"),
                "Transporte" => RedirectToAction("DashboardFlota",      "Reportes"),
                "Produccion" => RedirectToAction("DashboardProduccion", "Reportes"),
                "SSOMA"      => RedirectToAction("Index",               "Carros"),
                "Logistica"  => RedirectToAction("Dashboard",           "Reportes"),
                _            => RedirectToAction("Index", "Home")   // Admin y cualquier otro
            };
        }

        // ── LOGOUT ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var nombre = HttpContext.Session.GetString("UsuarioNombre") ?? "Usuario";
            await _auditoriaService.RegistrarAsync("Logout", "Usuario", null,
                $"Cerró sesión: {nombre}");
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // ── ACCESO DENEGADO ──────────────────────────────────────
        public IActionResult Denegado() => View();
    }
}
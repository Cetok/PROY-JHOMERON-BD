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

        private const int MaxIntentos    = 5;
        private const int MinutosBloqueo = 15;

        public AuthController(AppDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        // ── LOGIN GET ────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
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
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error     = "Por favor ingresa tu usuario y contraseña.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.username.ToLower() == username.ToLower());

            const string errorGenerico = "Usuario o contraseña incorrectos.";

            if (usuario == null)
            {
                await Task.Delay(300);
                ViewBag.Error     = errorGenerico;
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (usuario.bloqueadoHasta.HasValue && usuario.bloqueadoHasta > DateTime.Now)
            {
                var minutosRestantes = (int)Math.Ceiling(
                    (usuario.bloqueadoHasta.Value - DateTime.Now).TotalMinutes);

                ViewBag.Error     = $"Cuenta bloqueada por seguridad. Intenta en {minutosRestantes} minuto(s).";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (!usuario.activo)
            {
                ViewBag.Error     = "Tu cuenta está desactivada. Contacta al administrador.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

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

            HttpContext.Session.SetString("UsuarioId",       usuario.idUsuario.ToString());
            HttpContext.Session.SetString("UsuarioNombre",   usuario.nombreCompleto ?? usuario.username);
            HttpContext.Session.SetString("UsuarioUsername", usuario.username);
            HttpContext.Session.SetString("UsuarioRol",      usuario.rol);

            await _auditoriaService.RegistrarAsync("Login", "Usuario", usuario.idUsuario,
                $"Inicio de sesión: {usuario.username}");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // ── Redirigir según rol al Index de su módulo principal ──
            // danitza tiene rol Admin o equivalente, igual cae en el default
            if (usuario.username.Equals("danitza", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home");

            return usuario.rol switch
            {
                "SoporteTI"  => RedirectToAction("Index", "Equipos"),
                "Transporte" => RedirectToAction("Index", "Carros"),
                "Produccion" => RedirectToAction("Index", "Maquinas"),
                "SSOMA"      => RedirectToAction("Index", "Carros"),
                "Logistica"  => RedirectToAction("Index", "Chips"),
                _            => RedirectToAction("Index", "Home")   // Admin
            };
        }

        // ── LOGOUT ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var iaService = HttpContext.RequestServices.GetRequiredService<IAService>();
            var idStr = HttpContext.Session.GetString("UsuarioId");
            if (int.TryParse(idStr, out int idU))
                await iaService.CerrarSesionAsync(idU);

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
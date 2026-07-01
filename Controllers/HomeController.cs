using Microsoft.AspNetCore.Mvc;

namespace PROYJHOME2026.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var rol      = HttpContext.Session.GetString("UsuarioRol") ?? "";
            var username = HttpContext.Session.GetString("UsuarioUsername") ?? "";

            // danitza siempre va al dashboard nuevo, sin importar su rol
            if (username.Equals("danitza", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Dashboard");

            return rol switch
            {
                "SoporteTI"  => RedirectToAction("Index", "Equipos"),
                "Transporte" => RedirectToAction("Index", "Carros"),
                "Produccion" => RedirectToAction("Index", "Maquinas"),
                "SSOMA"      => RedirectToAction("Index", "Carros"),
                "Logistica"  => RedirectToAction("Index", "Chips"),
                _            => RedirectToAction("Index", "Dashboard")   // Admin
            };
        }
    }
}
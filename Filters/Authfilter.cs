using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PROYJHOME2026.Filters
{
    /// <summary>
    /// Filtro global que protege todas las rutas.
    /// Si no hay sesión activa redirige al Login.
    /// </summary>
    public class AuthFilter : IActionFilter
    {
        // Controladores y acciones que NO requieren login
        private static readonly HashSet<string> PublicControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Auth"
        };

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";

            // Si es ruta pública → dejar pasar
            if (PublicControllers.Contains(controller)) return;

            // Verificar sesión
            var usuarioId = context.HttpContext.Session.GetString("UsuarioId");

            if (string.IsNullOrEmpty(usuarioId))
            {
                // Guardar la URL que intentaba acceder
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

                context.Result = new RedirectToActionResult("Login", "Auth",
                    new { returnUrl });
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
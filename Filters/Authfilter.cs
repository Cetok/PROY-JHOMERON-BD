using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PROYJHOME2026.Filters
{
    /// <summary>
    /// Filtro global que protege todas las rutas.
    /// - Sin sesión → redirige al Login.
    /// - Con sesión pero sin permiso → redirige a Denegado.
    /// 
    /// Roles:
    ///   Admin      → acceso total
    ///   SoporteTI  → solo Equipos TI + reportes TI
    ///   Transporte → solo Flota Vehicular + reportes Flota
    /// </summary>
    public class AuthFilter : IActionFilter
    {
        // Rutas públicas (no requieren login)
        private static readonly HashSet<string> PublicControllers =
            new(StringComparer.OrdinalIgnoreCase) { "Auth" };

        // ── Permisos por rol ──────────────────────────────────────────
        // Formato: "Controlador" o "Controlador.Accion" (acción específica)

        private static readonly HashSet<string> PermisosSoporteTI =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Home NO incluido — Oliver va directo a /Reportes/Dashboard
                // Empleados — solo ver
                "Empleados.Index",
                "Empleados.Details",
                // Grupos — solo ver
                "Grupos.Index",
                "Grupos.Details",
                // Equipos TI — completo EXCEPTO Chips
                "Equipos",
                "TipoEquipos",
                // Chips NO incluido — Oliver no tiene acceso
                "Asignaciones",
                "Historiales",
                "Motivos",
                // Reportes TI
                "Reportes.Index",
                "Reportes.Dashboard",
                "Reportes.DashboardData",
                "Reportes.EquiposData",
                "Reportes.EquiposCsv",
                "Reportes.EquiposPdf",
                "Reportes.AsignacionesData",
                "Reportes.AsignacionesCsv",
                "Reportes.AsignacionesPdf",
                "Reportes.HistorialData",
                "Reportes.HistorialCsv",
                "Reportes.HistorialPdf",
                // Notificaciones propias
                "Notificaciones",
            };

        private static readonly HashSet<string> PermisosTransporte =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Home NO incluido — Silvana va directo a /Reportes/DashboardFlota
                // Empleados — solo ver
                "Empleados.Index",
                "Empleados.Details",
                // Grupos — solo ver
                "Grupos.Index",
                "Grupos.Details",
                // Flota Vehicular — completo
                "Carros",
                "MantenimientoCarros",
                "CarroEstadoLogs",
                "CarroConductorLog",
                "CarroAsesorios",
                "CarroModalidades",
                "CarroSeguros",
                "Seguros",
                "Asesorios",
                "Modalidades",
                "CheckListTransporte",
                "InspeccionBotiquinTransporte",
                "InspeccionBotiquinGrupo",
                "InspeccionExtintor",
                // Reportes Flota
                "Reportes.IndexFlota",
                "Reportes.VehiculosData",
                "Reportes.VehiculosCsv",
                "Reportes.VehiculosPdf",
                "Reportes.MantenimientoData",
                "Reportes.MantenimientoCsv",
                "Reportes.MantenimientoPdf",
                "Reportes.HistorialFlotaData",
                "Reportes.HistorialFlotaCsv",
                "Reportes.HistorialFlotaPdf",
                "Reportes.DashboardFlota",
                "Reportes.DashboardFlotaData",
                // Notificaciones propias
                "Notificaciones",
            };

        // Acciones bloqueadas para SoporteTI dentro de Empleados
        private static readonly HashSet<string> EmpleadosBloqueadosSoporteTI =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Create", "Edit", "Delete", "DeleteConfirmed", "CambiarEstado"
            };

        // Acciones bloqueadas para Transporte dentro de Empleados
        private static readonly HashSet<string> EmpleadosBloqueadosTransporte =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Create", "Edit", "Delete", "DeleteConfirmed", "CambiarEstado"
            };

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var ctrl   = context.RouteData.Values["controller"]?.ToString() ?? "";
            var accion = context.RouteData.Values["action"]?.ToString() ?? "";

            // 1. Ruta pública → pasar
            if (PublicControllers.Contains(ctrl)) return;

            // 2. Verificar sesión
            var usuarioId = context.HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioId))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                return;
            }

            var rol = context.HttpContext.Session.GetString("UsuarioRol") ?? "";

            // 3. Admin → acceso total
            if (rol.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return;

            // 4. Verificar permisos según rol
            var permitido = rol switch
            {
                "SoporteTI"  => TienePermiso(ctrl, accion, PermisosSoporteTI,  EmpleadosBloqueadosSoporteTI),
                "Transporte" => TienePermiso(ctrl, accion, PermisosTransporte, EmpleadosBloqueadosTransporte),
                _            => false
            };

            if (!permitido)
                context.Result = new RedirectToActionResult("Denegado", "Auth", null);
        }

        private static bool TienePermiso(
            string ctrl, string accion,
            HashSet<string> permisos,
            HashSet<string> bloqueadosEnEmpleados)
        {
            // Caso especial: Empleados con acciones bloqueadas
            if (ctrl.Equals("Empleados", StringComparison.OrdinalIgnoreCase) &&
                bloqueadosEnEmpleados.Contains(accion))
                return false;

            // Verificar "Controlador.Accion" primero (más específico)
            if (permisos.Contains($"{ctrl}.{accion}")) return true;

            // Luego verificar solo "Controlador" (acceso completo al controlador)
            if (permisos.Contains(ctrl)) return true;

            return false;
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
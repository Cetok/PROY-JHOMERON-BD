using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PROYJHOME2026.Filters
{
    public class AuthFilter : IActionFilter
    {
        private static readonly HashSet<string> PublicControllers =
            new(StringComparer.OrdinalIgnoreCase) { "Auth" };

        private static readonly HashSet<string> PermisosSoporteTI =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Equipos", "TipoEquipos", "Asignaciones", "Historiales", "Motivos",
                "Reportes.Index", "Reportes.Dashboard", "Reportes.DashboardData",
                "Reportes.EquiposData", "Reportes.EquiposCsv", "Reportes.EquiposPdf",
                "Reportes.AsignacionesData", "Reportes.AsignacionesCsv", "Reportes.AsignacionesPdf",
                "Reportes.HistorialData", "Reportes.HistorialCsv", "Reportes.HistorialPdf",
                "Notificaciones",
            };

        private static readonly HashSet<string> PermisosTransporte =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Carros", "MantenimientoCarros", "CarroEstadoLogs",
                "CarroConductorLog", "CarroAsesorios", "CarroModalidades",
                "CarroSeguros", "Seguros", "Asesorios", "Modalidades",
                "CheckListTransporte", "InspeccionBotiquinTransporte",
                "InspeccionBotiquinGrupo", "InspeccionExtintor",
                "Reportes.IndexFlota", "Reportes.VehiculosData",
                "Reportes.VehiculosCsv", "Reportes.VehiculosPdf",
                "Reportes.MantenimientoData", "Reportes.MantenimientoCsv", "Reportes.MantenimientoPdf",
                "Reportes.DashboardFlota", "Reportes.DashboardFlotaData",
                "Notificaciones",
            };

        private static readonly HashSet<string> PermisosProduccion =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Maquinas",
                "MaquinaAsignaciones",
                "Reportes.IndexProduccion",
                "Reportes.MaquinasData", "Reportes.MaquinasCsv", "Reportes.MaquinasPdf",
                "Reportes.AsignacionesProduccionData", "Reportes.AsignacionesProduccionCsv", "Reportes.AsignacionesProduccionPdf",
                "Reportes.HistorialMaquinasData", "Reportes.HistorialMaquinasCsv", "Reportes.HistorialMaquinasPdf",
                "Reportes.DashboardProduccion", "Reportes.DashboardProduccionData",
                "Notificaciones",
            };

        private static readonly HashSet<string> EmpleadosBloqueados =
            new(StringComparer.OrdinalIgnoreCase)
            { "Create", "Edit", "Delete", "DeleteConfirmed", "CambiarEstado" };

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var ctrl   = context.RouteData.Values["controller"]?.ToString() ?? "";
            var accion = context.RouteData.Values["action"]?.ToString()     ?? "";

            if (PublicControllers.Contains(ctrl)) return;

            var usuarioId = context.HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioId))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                return;
            }

            var rol = context.HttpContext.Session.GetString("UsuarioRol") ?? "";
            if (rol.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return;

            var permitido = rol switch
            {
                "SoporteTI"  => TienePermiso(ctrl, accion, PermisosSoporteTI),
                "Transporte" => TienePermiso(ctrl, accion, PermisosTransporte),
                "Produccion" => TienePermiso(ctrl, accion, PermisosProduccion),
                _            => false
            };

            if (!permitido)
                context.Result = new RedirectToActionResult("Denegado", "Auth", null);
        }

        private static bool TienePermiso(string ctrl, string accion, HashSet<string> permisos)
        {
            if (ctrl.Equals("Empleados", StringComparison.OrdinalIgnoreCase) &&
                EmpleadosBloqueados.Contains(accion))
                return false;

            if (permisos.Contains($"{ctrl}.{accion}")) return true;
            if (permisos.Contains(ctrl)) return true;
            return false;
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
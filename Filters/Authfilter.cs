using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PROYJHOME2026.Filters
{
    public class AuthFilter : IActionFilter
    {
        private static readonly HashSet<string> PublicControllers =
            new(StringComparer.OrdinalIgnoreCase) { "Auth" };

        // ── SoporteTI (Oliver) ────────────────────────────────────
        private static readonly HashSet<string> PermisosSoporteTI =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Equipos", "TipoEquipos",
                "Asignaciones", "Asignaciones.CargoPdf", "Historiales", "Motivos",
                "Notificaciones",
                "IA",
                "Dashboard",
            };

        // ── Transporte (Silvana, Ayde) ────────────────────────────
        private static readonly HashSet<string> PermisosTransporte =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Carros", "MantenimientoCarros", "CarroEstadoLogs",
                "CarroConductorLog", "CarroAsesorios", "CarroModalidades",
                "CarroSeguros", "Seguros", "Asesorios", "Modalidades",
                "TipoMantenimientos",
                "HabilitacionesVehiculares", "CertificadosCarro",
                "CheckListTransporte", "InspeccionBotiquinTransporte",
                "InspeccionBotiquinGrupo", "InspeccionExtintor",
                "Notificaciones",
                "IA",
                "Dashboard",
            };

        // ── Produccion (Eusebio) ──────────────────────────────────
        private static readonly HashSet<string> PermisosProduccion =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Maquinas", "MaquinaAsignaciones",
                "Notificaciones",
                "IA",
                "Dashboard",
            };

        // ── Logistica (Yanet) ─────────────────────────────────────
        private static readonly HashSet<string> PermisosLogistica =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Equipos.Index", "Equipos.Details",
                "Equipos.Create", "Equipos.Edit",
                "TipoEquipos.Index", "TipoEquipos.Details",
                "Chips",
                "Asignaciones.Index", "Asignaciones.Details",
                "Asignaciones.Create", "Asignaciones.Edit",
                "Historiales.Index", "Historiales.Details",
                "Notificaciones",
                "IA",
                "Dashboard",
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

            var username = context.HttpContext.Session.GetString("UsuarioUsername") ?? "";
            if (username.Equals("danitza", StringComparison.OrdinalIgnoreCase)) return;

            var permitido = rol switch
            {
                "SoporteTI"  => TienePermiso(ctrl, accion, PermisosSoporteTI),
                "Transporte" => TienePermiso(ctrl, accion, PermisosTransporte),
                "Produccion" => TienePermiso(ctrl, accion, PermisosProduccion),
                "SSOMA"      => TienePermisoSSoma(ctrl, accion),
                "Logistica"  => TienePermisoLogistica(ctrl, accion),
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

        private static readonly HashSet<string> AccionesSoloVer =
            new(StringComparer.OrdinalIgnoreCase) { "Index", "Details" };

        private static readonly HashSet<string> AccionesBloqueadasSSoma =
            new(StringComparer.OrdinalIgnoreCase) { "Create", "Edit", "Delete", "DeleteConfirmed", "CambiarEstado" };

        private static bool TienePermisoSSoma(string ctrl, string accion)
        {
            if (ctrl.Equals("IA",        StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Dashboard", StringComparison.OrdinalIgnoreCase)) return true;

            if (ctrl.Equals("Empleados", StringComparison.OrdinalIgnoreCase))
                return AccionesSoloVer.Contains(accion);

            if (ctrl.Equals("Carros", StringComparison.OrdinalIgnoreCase))
                return AccionesSoloVer.Contains(accion);

            if (ctrl.Equals("HabilitacionesVehiculares", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("CertificadosCarro",         StringComparison.OrdinalIgnoreCase)) return true;

            if (ctrl.Equals("Grupos", StringComparison.OrdinalIgnoreCase))
                return AccionesSoloVer.Contains(accion);

            if (ctrl.Equals("Asesorios", StringComparison.OrdinalIgnoreCase))
                return !AccionesBloqueadasSSoma.Contains(accion) || accion == "Index" || accion == "Details";

            if (ctrl.Equals("InspeccionBotiquinTransporte", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("InspeccionBotiquinGrupo",      StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("InspeccionExtintor",           StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("CarroAsesorios",               StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("GrupoAsesorios",               StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Notificaciones",               StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool TienePermisoLogistica(string ctrl, string accion)
        {
            if (ctrl.Equals("IA",        StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Dashboard", StringComparison.OrdinalIgnoreCase)) return true;

            var soloVer = new[] { "Index", "Details" };
            if (ctrl.Equals("Grupos",      StringComparison.OrdinalIgnoreCase) && soloVer.Contains(accion, StringComparer.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("TipoEquipos", StringComparison.OrdinalIgnoreCase) && soloVer.Contains(accion, StringComparer.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Chips",          StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Historiales",    StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Motivos",        StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Notificaciones", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Equipos",        StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Asignaciones",   StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
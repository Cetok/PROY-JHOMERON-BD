using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PROYJHOME2026.Filters
{
    public class AuthFilter : IActionFilter
    {
        private static readonly HashSet<string> PublicControllers =
            new(StringComparer.OrdinalIgnoreCase) { "Auth" };

        // ── SoporteTI (Oliver) — sin Chips, sin crear/editar Celulares ────────
        private static readonly HashSet<string> PermisosSoporteTI =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Equipos", "TipoEquipos",
                // Chips NO incluido — Oliver no tiene acceso
                "Asignaciones", "Historiales", "Motivos",
                "Reportes.Index", "Reportes.Dashboard", "Reportes.DashboardData",
                "Reportes.EquiposData", "Reportes.EquiposCsv", "Reportes.EquiposPdf",
                "Reportes.AsignacionesData", "Reportes.AsignacionesCsv", "Reportes.AsignacionesPdf",
                "Reportes.HistorialData", "Reportes.HistorialCsv", "Reportes.HistorialPdf",
                "Notificaciones",
            };

        // ── Transporte (Silvana) ───────────────────────────────────────────────
        private static readonly HashSet<string> PermisosTransporte =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Carros", "MantenimientoCarros", "CarroEstadoLogs",
                "CarroConductorLog", "CarroAsesorios", "CarroModalidades",
                "CarroSeguros", "Seguros", "Asesorios", "Modalidades",
                "TipoMantenimientos",
                "CheckListTransporte", "InspeccionBotiquinTransporte",
                "InspeccionBotiquinGrupo", "InspeccionExtintor",
                "Reportes.IndexFlota", "Reportes.VehiculosData",
                "Reportes.VehiculosCsv", "Reportes.VehiculosPdf",
                "Reportes.MantenimientoData", "Reportes.MantenimientoCsv", "Reportes.MantenimientoPdf",
                "Reportes.DashboardFlota", "Reportes.DashboardFlotaData",
                "Notificaciones",
            };

        // ── Produccion (Eusebio) ──────────────────────────────────────────────
        private static readonly HashSet<string> PermisosProduccion =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                "Maquinas", "MaquinaAsignaciones",
                "Reportes.IndexProduccion", "Reportes.HistorialProduccion",
                "Reportes.MaquinasData", "Reportes.MaquinasCsv", "Reportes.MaquinasPdf",
                "Reportes.HistorialMaquinasData", "Reportes.HistorialMaquinasCsv", "Reportes.HistorialMaquinasPdf",
                "Reportes.DashboardProduccion", "Reportes.DashboardProduccionData",
                "Notificaciones",
            };

        // ── Logistica (Yanet) — Equipos TI + Chips + solo Celulares + Reportes TI
        private static readonly HashSet<string> PermisosLogistica =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Empleados.Index", "Empleados.Details",
                "Grupos.Index", "Grupos.Details",
                // Equipos: ver todo, crear/editar solo Celulares (controlado por tipo en controller)
                "Equipos.Index", "Equipos.Details",
                "Equipos.Create", "Equipos.Edit",       // solo si es Celular (validado en controller)
                "TipoEquipos.Index", "TipoEquipos.Details",
                // Chips: completo
                "Chips",
                // Asignaciones: solo para Celulares
                "Asignaciones.Index", "Asignaciones.Details",
                "Asignaciones.Create", "Asignaciones.Edit",
                "Historiales.Index", "Historiales.Details",
                // Reportes TI: igual que Oliver
                "Reportes.Index", "Reportes.Dashboard", "Reportes.DashboardData",
                "Reportes.EquiposData", "Reportes.EquiposCsv", "Reportes.EquiposPdf",
                "Reportes.AsignacionesData", "Reportes.AsignacionesCsv", "Reportes.AsignacionesPdf",
                "Reportes.HistorialData", "Reportes.HistorialCsv", "Reportes.HistorialPdf",
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
                "SSOMA"      => TienePermisoSSoma(ctrl, accion),
                "Logistica"  => TienePermiso(ctrl, accion, PermisosLogistica),
                _            => false
            };

            if (!permitido)
                context.Result = new RedirectToActionResult("Denegado", "Auth", null);
        }

        private static bool TienePermiso(string ctrl, string accion, HashSet<string> permisos)
        {
            // Empleados: bloquear crear/editar para todos los no-Admin
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
            // Empleados: solo ver (Index + Details), sin crear/editar/eliminar
            if (ctrl.Equals("Empleados", StringComparison.OrdinalIgnoreCase))
                return AccionesSoloVer.Contains(accion);

            // Carros: solo ver (Index + Details)
            if (ctrl.Equals("Carros", StringComparison.OrdinalIgnoreCase))
                return AccionesSoloVer.Contains(accion);

            // Grupos: Index + Details (sin crear/editar), puede hacer inspecciones desde Details
            if (ctrl.Equals("Grupos", StringComparison.OrdinalIgnoreCase))
                return AccionesSoloVer.Contains(accion);

            // Asesorios: ver, crear, editar — no eliminar
            if (ctrl.Equals("Asesorios", StringComparison.OrdinalIgnoreCase))
                return !AccionesBloqueadasSSoma.Contains(accion) || accion == "Index" || accion == "Details";

            // Inspecciones: acceso completo
            if (ctrl.Equals("InspeccionBotiquinTransporte", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("InspeccionBotiquinGrupo", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("InspeccionExtintor", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("CarroAsesorios", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("GrupoAsesorios", StringComparison.OrdinalIgnoreCase)) return true;
            if (ctrl.Equals("Notificaciones", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
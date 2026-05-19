using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class MaquinasController : Controller
    {
        private readonly AppDbContext        _context;
        private readonly AuditoriaService    _auditoriaService;
        private readonly NotificacionService _notifService;

        public MaquinasController(AppDbContext context, AuditoriaService auditoriaService, NotificacionService notifService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
            _notifService     = notifService;
        }

        // ── INDEX ────────────────────────────────────────────────
        public async Task<IActionResult> Index(
            string? nombreMaquina, string? numeroDesde, string? numeroHasta,
            string? estado, string? marca, string? encargado, int pagina = 1)
        {
            int porPagina = 10;
            var query = _context.Maquinas
                .Include(m => m.Asignaciones.Where(a => a.EsActiva)).ThenInclude(a => a.Grupo)
                .Include(m => m.Asignaciones.Where(a => a.EsActiva)).ThenInclude(a => a.Encargados).ThenInclude(e => e.Empleado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(nombreMaquina))
                query = query.Where(m => m.NombreMaquina.Contains(nombreMaquina));

            if (!string.IsNullOrWhiteSpace(numeroDesde) && !string.IsNullOrWhiteSpace(numeroHasta))
                query = query.Where(m =>
                    m.NumeroMaquina.CompareTo(numeroDesde) >= 0 &&
                    m.NumeroMaquina.CompareTo(numeroHasta) <= 0);
            else if (!string.IsNullOrWhiteSpace(numeroDesde))
                query = query.Where(m => m.NumeroMaquina.Contains(numeroDesde));

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(m => m.Estado == estado);

            if (!string.IsNullOrWhiteSpace(marca))
                query = query.Where(m => m.Marca != null && m.Marca.Contains(marca));

            if (!string.IsNullOrWhiteSpace(encargado))
                query = query.Where(m => m.Asignaciones.Any(a => a.EsActiva &&
                    a.Encargados.Any(e =>
                        (e.Empleado.nombre != null && e.Empleado.nombre.Contains(encargado)) ||
                        (e.Empleado.paterno != null && e.Empleado.paterno.Contains(encargado)))));

            int total = await query.CountAsync();
            var listaMaquinas = await query.OrderBy(m => m.NumeroMaquina).ThenBy(m => m.Correlativo)
                .Skip((pagina - 1) * porPagina).Take(porPagina).ToListAsync();

            if (!string.IsNullOrWhiteSpace(numeroDesde) && !string.IsNullOrWhiteSpace(numeroHasta))
            {
                listaMaquinas = listaMaquinas
                    .Where(m =>
                        string.Compare(m.NumeroMaquina, numeroDesde, StringComparison.OrdinalIgnoreCase) >= 0 &&
                        string.Compare(m.NumeroMaquina, numeroHasta, StringComparison.OrdinalIgnoreCase) <= 0)
                    .ToList();
                total = listaMaquinas.Count;
            }

            ViewBag.NombreMaquina = nombreMaquina;
            ViewBag.NumeroDesde   = numeroDesde;
            ViewBag.NumeroHasta   = numeroHasta;
            ViewBag.Estado        = estado;
            ViewBag.Marca         = marca;
            ViewBag.Encargado     = encargado;
            ViewBag.Pagina        = pagina;
            ViewBag.Total         = total;
            ViewBag.TotalPaginas  = (int)Math.Ceiling((double)total / porPagina);
            return View(listaMaquinas);
        }

        // ── DETAILS ──────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var maquina = await _context.Maquinas
                .Include(m => m.Asignaciones).ThenInclude(a => a.Grupo)
                .Include(m => m.Asignaciones).ThenInclude(a => a.Encargados).ThenInclude(e => e.Empleado)
                .Include(m => m.Logs.OrderByDescending(l => l.FechaHora))
                .Include(m => m.CambiosAccesorios.OrderByDescending(c => c.FechaHora))
                .FirstOrDefaultAsync(m => m.IdMaquina == id);

            if (maquina == null) return NotFound();
            return View(maquina);
        }

        // ── CREATE GET ───────────────────────────────────────────
        public async Task<IActionResult> Create([FromQuery] string? numero)
        {
            if (!string.IsNullOrWhiteSpace(numero))
            {
                var totalExist = await _context.Maquinas.CountAsync(m => m.NumeroMaquina == numero);
                if (totalExist > 0)
                {
                    ViewBag.ProximoCorrelativo = await ObtenerProximoCorrelativo(numero);
                    ViewBag.NumeroBase = numero;
                }
                else
                {
                    ViewBag.ProximoCorrelativo = null; // primera de su serie
                    ViewBag.NumeroBase = numero;
                }
            }
            return View();
        }

        // ── CREATE POST ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Maquina maquina)
        {
            ModelState.Remove("AsignacionActual");
            ModelState.Remove("Logs");
            ModelState.Remove("Correlativo");
            ModelState.Remove("NumeroCompleto");
            maquina.FechaRegistro = DateTime.Now;

            var idStr = HttpContext.Session.GetString("UsuarioId");
            maquina.IdUsuarioCreador = int.TryParse(idStr, out int uid) ? uid : null;

            if (ModelState.IsValid)
            {
                // Ver cuántas máquinas existen con ese número base
                var existentes = await _context.Maquinas
                    .Where(m => m.NumeroMaquina == maquina.NumeroMaquina)
                    .OrderBy(m => m.IdMaquina)
                    .ToListAsync();

                if (existentes.Count == 0)
                {
                    // Primera de su serie — sin correlativo todavía
                    maquina.Correlativo = null;
                }
                else
                {
                    // Ya existe al menos una — asignar correlativos a todas
                    // Si la primera no tiene correlativo, asignarle el 01
                    int contador = 0;
                    foreach (var ex in existentes)
                    {
                        if (string.IsNullOrEmpty(ex.Correlativo))
                        {
                            contador++;
                            ex.Correlativo = contador.ToString("D2");
                        }
                        else if (int.TryParse(ex.Correlativo, out int n) && n > contador)
                        {
                            contador = n;
                        }
                        else
                        {
                            contador++;
                        }
                    }
                    contador++;
                    maquina.Correlativo = contador.ToString("D2");
                }

                _context.Add(maquina);
                await _context.SaveChangesAsync();

                await RegistrarLog(maquina.IdMaquina, "Edicion", null,
                    $"Estado inicial: {maquina.Estado}", "Máquina registrada en el sistema.");
                await _auditoriaService.RegistrarAsync("Crear", "Maquina", maquina.IdMaquina,
                    $"Registró máquina {maquina.NumeroCompleto} — {maquina.NombreMaquina}");
                await _notifService.NotificarAccionAsync("Creacion", "Máquina",
                    $"Se registró la máquina {maquina.NumeroCompleto} — {maquina.NombreMaquina}",
                    $"/Maquinas/Details/{maquina.IdMaquina}",
                    idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _mq1) ? _mq1 : null);

                TempData["Success"] = $"Máquina {maquina.NumeroCompleto} registrada correctamente.";
                return RedirectToAction(nameof(Details), new { id = maquina.IdMaquina });
            }

            // Calcular preview para mostrar en el form
            var totalExist = await _context.Maquinas.CountAsync(m => m.NumeroMaquina == maquina.NumeroMaquina);
            ViewBag.ProximoCorrelativo = totalExist > 0
                ? await ObtenerProximoCorrelativo(maquina.NumeroMaquina) : null;
            return View(maquina);
        }

        // ── EDIT GET ─────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var maquina = await _context.Maquinas.FirstOrDefaultAsync(m => m.IdMaquina == id);
            if (maquina == null) return NotFound();
            return View(maquina);
        }

        // ── EDIT POST ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Maquina maquina)
        {
            if (id != maquina.IdMaquina) return NotFound();
            ModelState.Remove("AsignacionActual");
            ModelState.Remove("Logs");
            ModelState.Remove("NumeroCompleto");

            if (ModelState.IsValid)
            {
                var anterior = await _context.Maquinas.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.IdMaquina == id);
                if (anterior == null) return NotFound();

                // Si cambió el número de máquina, recalcular correlativo
                if (anterior.NumeroMaquina != maquina.NumeroMaquina)
                    maquina.Correlativo = await ObtenerProximoCorrelativo(maquina.NumeroMaquina, idExcluir: id);
                else
                    maquina.Correlativo = anterior.Correlativo; // mantener el mismo

                // Preservar estado y baja (no se editan desde aquí)
                maquina.Estado      = anterior.Estado;
                maquina.FechaBaja   = anterior.FechaBaja;
                maquina.MotivoBaja  = anterior.MotivoBaja;

                try
                {
                    _context.Update(maquina);
                    await _context.SaveChangesAsync();

                    // Historial automático de accesorios
                    var accesorioAntes   = anterior.AccesoriosParte?.Trim() ?? "";
                    var accesorioDespues = maquina.AccesoriosParte?.Trim() ?? "";
                    if (accesorioAntes != accesorioDespues)
                    {
                        var nombre = HttpContext.Session.GetString("UsuarioNombre");
                        _context.MaquinaAccesorioCambios.Add(new MaquinaAccesorioCambio
                        {
                            IdMaquina         = id,
                            NombreAccesorio   = "Accesorios / Partes",
                            AccesorioAnterior = string.IsNullOrWhiteSpace(accesorioAntes)   ? "—" : accesorioAntes,
                            AccesorioNuevo    = string.IsNullOrWhiteSpace(accesorioDespues) ? "—" : accesorioDespues,
                            Observaciones     = "Cambio registrado automáticamente al editar la máquina.",
                            IdUsuario         = int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int uidAcc) ? uidAcc : null,
                            NombreUsuario     = nombre,
                            FechaHora         = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    // Logs de campos cambiados
                    var cambios = new List<(string campo, string? antes, string? despues)>
                    {
                        ("N° Máquina",    anterior.NumeroMaquina,                          maquina.NumeroMaquina),
                        ("Nombre",        anterior.NombreMaquina,                          maquina.NombreMaquina),
                        ("Marca",         anterior.Marca,                                  maquina.Marca),
                        ("Fecha Compra",  anterior.FechaCompra?.ToString("dd/MM/yyyy"),    maquina.FechaCompra?.ToString("dd/MM/yyyy")),
                        ("Observaciones", anterior.Observaciones,                          maquina.Observaciones),
                    };

                    foreach (var (campo, antes, despues) in cambios)
                    {
                        var a = string.IsNullOrWhiteSpace(antes)   ? "—" : antes.Trim();
                        var d = string.IsNullOrWhiteSpace(despues) ? "—" : despues.Trim();
                        if (a != d)
                            await RegistrarLog(id, "Edicion", $"{campo}: {a}", $"{campo}: {d}", $"Campo '{campo}' modificado.");
                    }

                    await _auditoriaService.RegistrarAsync("Editar", "Maquina", id, $"Editó máquina {maquina.NumeroCompleto}");
                    TempData["Success"] = "Máquina actualizada correctamente.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Maquinas.AnyAsync(m => m.IdMaquina == id)) return NotFound();
                    throw;
                }
            }
            return View(maquina);
        }

        // ── CAMBIAR ESTADO POST ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int idMaquina, string nuevoEstado, string? observaciones)
        {
            var maquina = await _context.Maquinas
                .Include(m => m.Asignaciones)
                .FirstOrDefaultAsync(m => m.IdMaquina == idMaquina);
            if (maquina == null) return NotFound();

            // Bloquear si ya está dado de baja
            if (maquina.Estado == "Dado de Baja")
            {
                TempData["Error"] = "Esta máquina está dada de baja y no puede cambiar de estado.";
                return RedirectToAction(nameof(Details), new { id = idMaquina });
            }

            var estadoAnterior = maquina.Estado;
            maquina.Estado = nuevoEstado;

            // Al terminar mantenimiento → volver al estado que tenía la asignación
            if (nuevoEstado == "Activo" && maquina.AsignacionActual != null)
                maquina.AsignacionActual.EstadoOperativo = "Operativo";

            // Al entrar a mantenimiento → reflejar en asignación
            if (nuevoEstado == "Mantenimiento" && maquina.AsignacionActual != null)
                maquina.AsignacionActual.EstadoOperativo = "Mantenimiento";

            await _context.SaveChangesAsync();

            await RegistrarLog(idMaquina, "CambioEstado", estadoAnterior, nuevoEstado,
                observaciones ?? $"Estado cambiado a {nuevoEstado}");
            await _auditoriaService.RegistrarAsync("CambioEstado", "Maquina", idMaquina,
                $"Máquina #{idMaquina}: {estadoAnterior} → {nuevoEstado}");
            await _notifService.NotificarAccionAsync("CambioEstado", "Máquina",
                $"Máquina {maquina.NumeroCompleto} cambió a estado: {nuevoEstado}",
                $"/Maquinas/Details/{idMaquina}",
                idUsuarioAccion: int.TryParse(HttpContext.Session.GetString("UsuarioId"), out int _mq) ? _mq : null);

            TempData["Success"] = $"Estado cambiado a '{nuevoEstado}'.";
            return RedirectToAction(nameof(Details), new { id = idMaquina });
        }

        // ── DAR DE BAJA POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DarDeBaja(int idMaquina, string motivoBaja, DateTime? fechaBaja)
        {
            var maquina = await _context.Maquinas
                .Include(m => m.Asignaciones)
                .FirstOrDefaultAsync(m => m.IdMaquina == idMaquina);
            if (maquina == null) return NotFound();

            if (string.IsNullOrWhiteSpace(motivoBaja))
            {
                TempData["Error"] = "Debe indicar el motivo de la baja.";
                return RedirectToAction(nameof(Details), new { id = idMaquina });
            }

            var estadoAnterior = maquina.Estado;
            maquina.Estado     = "Dado de Baja";
            maquina.FechaBaja  = fechaBaja ?? DateTime.Today;
            maquina.MotivoBaja = motivoBaja.Trim();

            // Cerrar asignación activa si existe
            var asigActiva = maquina.Asignaciones.FirstOrDefault(a => a.EsActiva);
            if (asigActiva != null)
            {
                asigActiva.EsActiva        = false;
                asigActiva.EstadoOperativo = "Inoperativo";
            }

            await _context.SaveChangesAsync();

            await RegistrarLog(idMaquina, "CambioEstado", estadoAnterior, "Dado de Baja",
                $"BAJA: {motivoBaja}");
            await _auditoriaService.RegistrarAsync("DarDeBaja", "Maquina", idMaquina,
                $"Máquina {maquina.NumeroCompleto} dada de baja. Motivo: {motivoBaja}");

            TempData["Success"] = $"Máquina {maquina.NumeroCompleto} dada de baja correctamente.";
            return RedirectToAction(nameof(Details), new { id = idMaquina });
        }

        // ── DELETE ───────────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var maquina = await _context.Maquinas.FirstOrDefaultAsync(m => m.IdMaquina == id);
            if (maquina == null) return NotFound();
            return View(maquina);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var maquina = await _context.Maquinas.FirstOrDefaultAsync(m => m.IdMaquina == id);
            if (maquina == null) return NotFound();
            try
            {
                _context.Maquinas.Remove(maquina);
                await _context.SaveChangesAsync();
                await _auditoriaService.RegistrarAsync("Eliminar", "Maquina", id,
                    $"Eliminó máquina {maquina.NumeroCompleto}");
                TempData["Success"] = $"Máquina {maquina.NumeroCompleto} eliminada.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar esta máquina porque tiene registros asociados.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return RedirectToAction(nameof(Index));
        }

        // ── ASIGNAR CORRELATIVOS A MÁQUINAS EXISTENTES ──────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarCorrelativos()
        {
            var sinCorrelativo = await _context.Maquinas
                .Where(m => m.Correlativo == null || m.Correlativo == "")
                .OrderBy(m => m.NumeroMaquina).ThenBy(m => m.IdMaquina)
                .ToListAsync();

            // Agrupar por número base y asignar correlativos en orden
            var grupos = sinCorrelativo.GroupBy(m => m.NumeroMaquina);
            foreach (var grupo in grupos)
            {
                // Ver el máximo correlativo numérico ya existente en esa serie
                var existentes = await _context.Maquinas
                    .Where(m => m.NumeroMaquina == grupo.Key && m.Correlativo != null && m.Correlativo != "")
                    .Select(m => m.Correlativo).ToListAsync();

                int maximo = 0;
                foreach (var c in existentes)
                    if (int.TryParse(c, out int n) && n > maximo)
                        maximo = n;

                foreach (var maq in grupo)
                {
                    maximo++;
                    maq.Correlativo = maximo.ToString("D2");
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Se asignaron correlativos a {sinCorrelativo.Count} máquina(s).";
            return RedirectToAction(nameof(Index));
        }

        // ── API: próximo correlativo (AJAX desde Create) ─────────
        [HttpGet]
        public async Task<IActionResult> ProximoCorrelativo(string numero)
        {
            var totalExist = await _context.Maquinas.CountAsync(m => m.NumeroMaquina == numero);
            if (totalExist == 0)
                return Json(new { correlativo = (string?)null, completo = numero, esPrimera = true });

            var proximo = await ObtenerProximoCorrelativo(numero);
            return Json(new { correlativo = proximo, completo = $"{numero}-{proximo}", esPrimera = false });
        }

        // ── HELPER: calcular próximo correlativo ─────────────────
        private async Task<string> ObtenerProximoCorrelativo(string numeroBase, int? idExcluir = null)
        {
            var query = _context.Maquinas.Where(m => m.NumeroMaquina == numeroBase);
            if (idExcluir.HasValue)
                query = query.Where(m => m.IdMaquina != idExcluir.Value);

            var correlativos = await query.Select(m => m.Correlativo).ToListAsync();
            var total        = correlativos.Count;

            int maximo = 0;
            foreach (var c in correlativos)
                if (int.TryParse(c, out int n) && n > maximo)
                    maximo = n;

            // El siguiente es el máximo entre el total existente y el máximo correlativo
            int siguiente = Math.Max(total, maximo) + 1;
            return siguiente.ToString("D2");
        }

        // ── HELPER: registrar log ────────────────────────────────
        private async Task RegistrarLog(int idMaquina, string tipoEvento,
            string? valorAnterior, string? valorNuevo, string? observaciones)
        {
            var idStr  = HttpContext.Session.GetString("UsuarioId");
            var nombre = HttpContext.Session.GetString("UsuarioNombre");
            _context.MaquinaLogs.Add(new MaquinaLog
            {
                IdMaquina     = idMaquina,
                IdUsuario     = int.TryParse(idStr, out int uid) ? uid : null,
                NombreUsuario = nombre,
                TipoEvento    = tipoEvento,
                ValorAnterior = valorAnterior,
                ValorNuevo    = valorNuevo,
                Observaciones = observaciones,
                FechaHora     = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        public IActionResult Historial()
        {
            ViewData["Title"]      = "Historial de Máquinas";
            ViewData["Breadcrumb"] = "Producción / Historial";
            return View();
        }
    }
}
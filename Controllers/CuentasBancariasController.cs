using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using PROYJHOME2026.Services;

namespace PROYJHOME2026.Controllers
{
    public class CuentasBancariasController : Controller
    {
        private readonly AppDbContext     _context;
        private readonly AuditoriaService _auditoriaService;

        public CuentasBancariasController(AppDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        // ── CREAR ─────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int idEmpleado, string tipoBanco,
            string? tipoCuenta, string numeroCuenta, string? numeroCCI)
        {
            if (string.IsNullOrWhiteSpace(tipoBanco) || string.IsNullOrWhiteSpace(numeroCuenta))
            {
                TempData["Error"] = "El banco y el número de cuenta son obligatorios.";
                return RedirectToAction("Details", "Empleados", new { id = idEmpleado });
            }

            var cuenta = new CuentaBancaria
            {
                IdEmpleado   = idEmpleado,
                TipoBanco    = tipoBanco.Trim(),
                TipoCuenta   = tipoCuenta?.Trim(),
                NumeroCuenta = numeroCuenta.Trim(),
                NumeroCCI    = numeroCCI?.Trim(),
                FechaRegistro = DateTime.Now,
            };

            _context.CuentasBancarias.Add(cuenta);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Crear", "CuentaBancaria", cuenta.IdCuenta,
                $"Registró cuenta {tipoBanco} N°{numeroCuenta} para empleado #{idEmpleado}");

            TempData["Success"] = "Cuenta bancaria registrada correctamente.";
            return RedirectToAction("Details", "Empleados", new { id = idEmpleado });
        }

        // ── EDITAR GET ────────────────────────────────────────────
        public async Task<IActionResult> Editar(int id)
        {
            var cuenta = await _context.CuentasBancarias
                .Include(c => c.Empleado)
                .FirstOrDefaultAsync(c => c.IdCuenta == id);
            if (cuenta == null) return NotFound();
            return View(cuenta);
        }

        // ── EDITAR POST ───────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string tipoBanco,
            string? tipoCuenta, string numeroCuenta, string? numeroCCI)
        {
            var cuenta = await _context.CuentasBancarias.FindAsync(id);
            if (cuenta == null) return NotFound();

            // Guardar valores anteriores para historial
            var anterior = $"Banco: {cuenta.TipoBanco} | Tipo: {cuenta.TipoCuenta} | N°: {cuenta.NumeroCuenta} | CCI: {cuenta.NumeroCCI}";

            cuenta.TipoBanco    = tipoBanco.Trim();
            cuenta.TipoCuenta   = tipoCuenta?.Trim();
            cuenta.NumeroCuenta = numeroCuenta.Trim();
            cuenta.NumeroCCI    = numeroCCI?.Trim();

            var nuevo = $"Banco: {cuenta.TipoBanco} | Tipo: {cuenta.TipoCuenta} | N°: {cuenta.NumeroCuenta} | CCI: {cuenta.NumeroCCI}";

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Editar", "CuentaBancaria", id,
                $"Empleado #{cuenta.IdEmpleado} — Anterior: [{anterior}] → Nuevo: [{nuevo}]");

            TempData["Success"] = "Cuenta bancaria actualizada.";
            return RedirectToAction("Details", "Empleados", new { id = cuenta.IdEmpleado });
        }

        // ── ELIMINAR ──────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cuenta = await _context.CuentasBancarias.FindAsync(id);
            if (cuenta == null) return NotFound();

            var idEmpleado = cuenta.IdEmpleado;
            _context.CuentasBancarias.Remove(cuenta);
            await _context.SaveChangesAsync();

            await _auditoriaService.RegistrarAsync("Eliminar", "CuentaBancaria", id,
                $"Eliminó cuenta {cuenta.TipoBanco} N°{cuenta.NumeroCuenta} del empleado #{idEmpleado}");

            TempData["Success"] = "Cuenta bancaria eliminada.";
            return RedirectToAction("Details", "Empleados", new { id = idEmpleado });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using System.Linq;

namespace PROYJHOME2026.Controllers
{
    public class EmpleadosController : Controller
    {
        private readonly AppDbContext _context;
        public EmpleadosController(AppDbContext context)
        {
            _context=context;
        }
        public IActionResult Index()
        {
            var empleados= _context.Empleados.ToList();
            return View(empleados);    
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Empleado empleado)
        {
            if (ModelState.IsValid)
            {
                _context.Empleados.Add(empleado);  
                _context.SaveChanges();
                return RedirectToAction("Index");  
            }
            return View(empleado);
        }
    }
}
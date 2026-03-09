using Microsoft.AspNetCore.Mvc;
using PROYJHOME2026.Data;
using PROYJHOME2026.Models;
using System.Linq;

namespace PROYJHOME2026.Controllers
{
    public class EquiposController : Controller
    {
        private readonly AppDbContext _context;
         public EquiposController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var equipos = _context.Equipos.ToList();
            return View(equipos);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Equipo equipo)
        {
            if (ModelState.IsValid)
            {
                _context.Equipos.Add(equipo);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(equipo);
        }
    }
}
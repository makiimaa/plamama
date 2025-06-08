using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using final.Models;
using final.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace final.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExerController : Controller
    {
        private readonly DataContext _context;

        public ExerController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (!Functions.IsLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var eList = _context.Exers.OrderBy(e => e.ExerId).ToList();
            return View(eList);
        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            var e = _context.Exers.Find(id);
            if (e == null)
                return NotFound();
            return View(e);
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var delExer = _context.Exers.Find(id);
            if (delExer == null)
                return NotFound();
            _context.Exers.Remove(delExer);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(tblExer exer)
        {
            if (ModelState.IsValid)
            {
                _context.Exers.Add(exer);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(exer);
        }
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            var e = _context.Exers.Find(id);
            if (e == null)
                return NotFound();
            return View(e);
        }
        [HttpPost]
        public IActionResult Edit(tblExer exer)
        {
            if (ModelState.IsValid)
            {
                _context.Exers.Update(exer);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(exer);
        }
    }
}
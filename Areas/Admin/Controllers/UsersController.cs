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
    public class UsersController : Controller
    {
        private readonly DataContext _context;
        public UsersController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (!Functions.IsLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var uList = _context.Userss.Where(u => u.Roles == 1).OrderBy(u => u.UserId).ToList();
            return View(uList);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(tblUsers users)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(users.Passwords))
                {
                    Functions._Message = "Mật khẩu không được để trống.";
                    return RedirectToAction("Create", "Users");
                }
                var check = _context.Userss.Where(u => (u.UserName == users.UserName)).FirstOrDefault();
                if (check != null)
                {
                    Functions._Message = "UserName already exists.";
                    return RedirectToAction("Create", "Users");
                }
                users.Roles = 1;
                users.Images = "default_avt.jpg";
                users.T1 = "Pro Dev";
                users.T2 = "Super Handsome";
                users.T3 = "Death Gamer";
                users.T4 = "God Tier";
                users.Bgr = "915bcb30ab2d16c5cbcf2ccde09ebe44.jpg";
                users.CreatedAt = DateTime.Now;
                Functions._Message = string.Empty;
                users.Passwords = Functions.MD5Password(users.Passwords);
                _context.Userss.Add(users);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(users);
        }
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            var e = _context.Userss.Find(id);
            if (e == null)
                return NotFound();
            return View(e);
        }
        [HttpPost]
        public IActionResult Edit(tblUsers users)
        {
            if (ModelState.IsValid)
            {
                users.Roles = 1;
                users.Images = "default_avt.jpg";
                users.T1 = "Pro Dev";
                users.T2 = "Super Handsome";
                users.T3 = "Death Gamer";
                users.T4 = "God Tier";
                users.Bgr = "915bcb30ab2d16c5cbcf2ccde09ebe44.jpg";
                users.CreatedAt = DateTime.Now;
                _context.Userss.Update(users);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(users);
        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            var e = _context.Userss.Find(id);
            if (e == null)
                return NotFound();
            return View(e);
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var delExer = _context.Userss.Find(id);
            if (delExer == null)
                return NotFound();
            _context.Userss.Remove(delExer);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
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
    public class RegisterController : Controller
    {
        private readonly DataContext _context;
        public RegisterController(DataContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Index(tblUsers users)
        {
            if (users == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(users.Passwords))
            {
                Functions._Message = "Mật khẩu không được để trống.";
                return RedirectToAction("Index", "Register");
            }
            var check = _context.Userss.Where(u => (u.UserName == users.UserName)).FirstOrDefault();
            if (check != null)
            {
                Functions._Message = "UserName already exists.";
                return RedirectToAction("Index", "Register");
            }
            Functions._Message = string.Empty;
            users.Passwords = Functions.MD5Password(users.Passwords);
            _context.Userss.Add(users);
            _context.SaveChanges();
            return RedirectToAction("Index","Login");
        }
    }
}
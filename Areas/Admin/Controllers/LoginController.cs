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
    public class LoginController : Controller
    {
        private readonly DataContext _context;
        public LoginController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Index(tblUsers users)
        {
            if (users == null)
            {
                return NotFound();
            }
            string pw = Functions.MD5Password(users.Passwords);
            var check = _context.Userss.Where(u => (u.UserName  == users.UserName) && (u.Passwords == pw)).FirstOrDefault();
            if (check == null)
            {
                Functions._Message = "Invalid username and password.";
                return RedirectToAction("Index", "Login");
            }
            Functions._Message = string.Empty;
            Functions._UserId = check.UserId;
            Functions._UserName = string.IsNullOrEmpty(check.UserName) ? string.Empty : check.UserName;
            Functions._Email = string.IsNullOrEmpty(check.Email) ? string.Empty: check.Email;
            if (check.Roles == 0)
            {
                return RedirectToAction("Index", "Home"); // Trang Admin trong Admin Area
            }
            else if (check.Roles == 1)
            {
                return Redirect("http://localhost:5262/"); // Trang client
            }
            else
            {
                Functions._Message = "Role không xác định.";
                return RedirectToAction("Index", "Login");
            }
                }
            }
}
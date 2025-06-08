using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using final.Models;
using Microsoft.AspNetCore.Mvc;

namespace final.Components
{
    [ViewComponent(Name = "HomeView")]
    public class HomeComponent : ViewComponent
    {
        private readonly DataContext _context;
        public HomeComponent(DataContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var listOfUsers = (from u in _context.Userss
                                where (u.Roles == 1) && (u.Statuss == 1)
                                select u).ToList();
            return await Task.FromResult((IViewComponentResult)View("Default", listOfUsers));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using final.Models;
using Microsoft.AspNetCore.Mvc;

namespace final.Components
{
    [ViewComponent(Name = "EUView")]
    public class EUComponent : ViewComponent
    {
        private readonly DataContext _context;
        public EUComponent(DataContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var elist = (from m in _context.vEUs
                            select m).ToList();
            return await Task.FromResult((IViewComponentResult)View("Default", elist));
        }
    }
}
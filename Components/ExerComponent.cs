using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using final.Models;
using final.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace final.Components
{
    [ViewComponent(Name = "ExerView")]
    public class ExerComponent : ViewComponent
    {
        private readonly DataContext _context;
        
        public ExerComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Lấy UID của user hiện tại
            int currentUserId = Functions._UserId;

            var exercises = await (from exer in _context.Exers
                                 where exer.IsActive == true
                                 select new ExerciseViewModel
                                 {
                                     ExerId = exer.ExerId,
                                     ExerName = exer.ExerName,
                                     Contents = exer.Contents,
                                     Deadline = exer.Deadline,
                                     CreateAt = exer.CreateAt,
                                     Images = exer.Images,
                                     Request = exer.Request,
                                     IsSubmitted = _context.EUs.Any(eu => eu.EID == exer.ExerId && eu.UID == currentUserId),
                                     IsOverdue = exer.Deadline.HasValue && exer.Deadline.Value < DateTime.Now
                                 }).OrderByDescending(x => x.CreateAt) // Sắp xếp từ mới đến cũ
                                 .ToListAsync();

            return await Task.FromResult((IViewComponentResult)View("Default", exercises));
        }
    }
}
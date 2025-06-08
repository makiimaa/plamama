using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using final.Areas.Admin.Models;

namespace final.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }
        public DbSet<tblUsers> Userss { get; set; }
        public DbSet<tblEU> EUs { get; set; }
        public DbSet<viewEU> vEUs { get; set; }
        public DbSet<tblExer> Exers { get; set; }
        public DbSet<AdminMenu> AdminMenus { get; set; }
    }
}
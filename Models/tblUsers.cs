using System.Net.Mime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace final.Models
{
    [Table("tblUsers")]
    public class tblUsers
    {
        [Key]
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? Passwords { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int Roles { get; set; }
        public int Statuss { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Images { get; set; }
        public string? T1 { get; set; }
        public string? T2 { get; set; }
        public string? T3 { get; set; }
        public string? T4 { get; set; }
        public string? Bgr { get; set; }
    }
}
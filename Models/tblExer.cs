using System.Net.Mime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace final.Models
{
    [Table("tblExer")]
    public class tblExer
    {
        [Key]
        public int ExerId { get; set; }
        public string? ExerName { get; set; }
        public string? Contents { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? Deadline { get; set; }
        public bool? Status { get; set; }
        public string? Images { get; set; }
        public string? Request { get; set; }
    }
}
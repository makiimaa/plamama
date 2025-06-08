using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace final.Models
{
    [Table("tblEU")]
    public class tblEU
    {
        [Key]
        public int EUID { get; set; }
        public int UID { get; set; }
        public int EID { get; set; }
        public string? FilePath { get; set; } = "";
        public string? Links { get; set; }
        public bool? IsAcepted { get; set; }
        public DateTime? SubmitDate { get; set; }

    }
}
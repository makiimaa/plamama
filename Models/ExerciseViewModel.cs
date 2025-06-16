using System;

namespace final.Models
{
    public class ExerciseViewModel
    {
        public int ExerId { get; set; }
        public string? ExerName { get; set; }
        public string? Contents { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Images { get; set; }
        public string? Request { get; set; }
        public bool IsSubmitted { get; set; }
        public bool IsOverdue { get; set; }
        
        // Thuộc tính để xác định màu hiển thị
        public string GetStatusColor()
        {
            if (IsSubmitted)
                return "success"; // Xanh lá cây - đã nộp
            else if (IsOverdue)
                return "danger"; // Đỏ - quá hạn chưa nộp
            else
                return "info"; // Xanh nước biển - chưa hết hạn chưa nộp
        }
        
        public string GetStatusText()
        {
            if (IsSubmitted)
                return "Submited";
            else if (IsOverdue)
                return "Out of Date";
            else
                return "Not yet";
        }
    }
}
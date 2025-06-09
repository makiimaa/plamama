using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace final.Models
{
    public class DuplicateResult
    {
        public int EUID { get; set; }
        public string FullName { get; set; } = "";
        public double SimilarityPercentage { get; set; }
        public string SimilarityLevel { get; set; } = ""; // Thấp, Trung bình, Cao, Rất cao
        public List<string> SimilarSentences { get; set; } = new List<string>();
    }

    public class DuplicateCheckRequest
    {
        public string Text1 { get; set; } = "";
        public string Text2 { get; set; } = "";
    }

    public class DuplicateCheckResponse
    {
        public double SimilarityScore { get; set; }
        public string Analysis { get; set; } = "";
        public List<string> SimilarSections { get; set; } = new List<string>();
    }

    public class DocumentComparisonResult
    {
        public int CurrentEUID { get; set; }
        public string CurrentUserName { get; set; } = "";
        public int EID { get; set; }
        public string ExerciseName { get; set; } = "";
        public List<DuplicateResult> Comparisons { get; set; } = new List<DuplicateResult>();
        public bool IsOnlySubmission { get; set; }
        public int TotalSubmissions { get; set; }
    }
}
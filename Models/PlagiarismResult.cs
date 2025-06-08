using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace final.Models
{
    public class PlagiarismResult
    {
        public string OriginalContent { get; set; } = "";
        public double PlagiarismPercentage { get; set; }
        public List<PlagiarizedSentence> PlagiarizedSentences { get; set; } = new();
    }

    public class PlagiarizedSentence
    {
        public string Sentence { get; set; } = "";
        public int ParagraphIndex { get; set; }
        public int SentenceIndex { get; set; }
        public int SourceCount { get; set; }
        public List<string> Sources { get; set; } = new();
    }
}
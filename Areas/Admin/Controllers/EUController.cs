using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using final.Models;
using Microsoft.AspNetCore.Mvc;
using final.Utilities;
using Microsoft.Extensions.Logging;

namespace final.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EUController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;
        public EUController(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        

        public IActionResult Index()
        {
            if (!Functions.IsLogin())
            {
                return RedirectToAction("Index", "Login");
            }
            var eU = _context.vEUs.OrderBy(e => e.EUID).ToList();
            return View(eU);
        }

        public IActionResult Details(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            var e = _context.vEUs.Find(id);
            if (e == null)
                return NotFound();
            return View(e);
        }
        [HttpPost]
        public async Task<IActionResult> Summarize(int EUID)
        {
            var eu = _context.vEUs.Find(EUID);
            if (eu == null) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", (eu.FilePath ?? "").TrimStart('/'));
            var sentences = Functions.ExtractSentencesFromPdf(filePath);
            var fullText = string.Join(" ", sentences);

            var apiKey = _configuration["Gemini:ApiKey"];
            var summary = await Functions.SummarizeWithGemini(fullText, apiKey!);

            ViewBag.Original = fullText;
            ViewBag.Summary = summary;
            return View("Summary", eu);
        }
        [HttpPost]
        public async Task<IActionResult> CheckPlagiarism(int EUID)
        {
            var eu = _context.vEUs.Find(EUID);
            if (eu == null) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", (eu.FilePath ?? "").TrimStart('/'));
            var paragraphs = Functions.ExtractParagraphsFromPdf(filePath);

            string apiKey = _configuration["GoogleSearch:ApiKey"]!;
            string cx = _configuration["GoogleSearch:CX"]!;

            var results = new List<(string paragraph, bool isPlagiarized, int matchCount, List<string> urls)>();

            foreach (var para in paragraphs)
            {
                var (matchCount, urls) = await Functions.CheckPlagiarismGoogle(para, apiKey, cx);
                results.Add((para, matchCount > 0, matchCount, urls));
            }

            ViewBag.Results = results;

            int plagiarizedCount = results.Count(r => r.isPlagiarized);
            float percentage = (results.Count > 0) ? (plagiarizedCount * 100f / results.Count) : 0f;
            ViewBag.PlagiarismRate = percentage;

            return View("Plagiarism", eu);
        }

    }
}
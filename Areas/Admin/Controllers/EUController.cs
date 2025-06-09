using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using final.Models;
using Microsoft.AspNetCore.Mvc;
using final.Utilities;
using Microsoft.Extensions.Logging;

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public IActionResult GetDashboardData()
        {
            try
            {
                if (!Functions.IsLogin())
                {
                    return Unauthorized();
                }

                // Debug: Kiểm tra dữ liệu thô
                var allRecords = _context.vEUs
                    .Where(eu => eu.SubmitDate.HasValue)
                    .Select(eu => new
                    {
                        eu.SubmitDate,
                        eu.FullName,
                        eu.ExerName,
                        eu.UID
                    })
                    .ToList();

                Console.WriteLine("=== All Records with SubmitDate ===");
                foreach (var record in allRecords)
                {
                    Console.WriteLine($"SubmitDate: {record.SubmitDate}, Name: {record.FullName}, Exercise: {record.ExerName}");
                }

                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                Console.WriteLine($"Current DateTime: {DateTime.Now}");
                Console.WriteLine($"Seven days ago: {sevenDaysAgo}");

                // Debug: Kiểm tra records trong 7 ngày
                var recordsInLast7Days = _context.vEUs
                    .Where(eu => eu.SubmitDate.HasValue && eu.SubmitDate >= sevenDaysAgo)
                    .ToList();

                Console.WriteLine($"Records in last 7 days: {recordsInLast7Days.Count}");

                // Dữ liệu cho biểu đồ - thống kê theo ngày trong 7 ngày gần nhất
                var dailyStats = new List<object>();

                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Now.AddDays(-i).Date;
                    var nextDate = date.AddDays(1);

                    Console.WriteLine($"Checking date range: {date} to {nextDate}");

                    // Đếm tổng số bài nộp trong ngày
                    var daySubmissions = _context.vEUs
                        .Where(eu => eu.SubmitDate >= date && eu.SubmitDate < nextDate)
                        .ToList(); // Debug: lấy về list để xem dữ liệu

                    Console.WriteLine($"Submissions on {date.ToString("yyyy-MM-dd")}: {daySubmissions.Count}");
                    foreach (var sub in daySubmissions)
                    {
                        Console.WriteLine($"  - SubmitDate: {sub.SubmitDate}, Name: {sub.FullName}");
                    }

                    var totalSubmissions = daySubmissions.Count;

                    // Đếm tổng số người nộp trong ngày (distinct by UID)
                    var totalUsers = daySubmissions
                        .Select(eu => eu.UID)
                        .Distinct()
                        .Count();

                    Console.WriteLine($"Total users on {date.ToString("yyyy-MM-dd")}: {totalUsers}");

                    dailyStats.Add(new
                    {
                        date = date.ToString("yyyy-MM-dd"),
                        submissions = totalSubmissions,
                        users = totalUsers
                    });
                }

                Console.WriteLine("=== Daily Stats ===");
                foreach (var stat in dailyStats)
                {
                    Console.WriteLine($"Date: {stat}, Submissions: {((dynamic)stat).submissions}, Users: {((dynamic)stat).users}");
                }

                // Lấy 10 hoạt động gần nhất
                var recentActivities = _context.vEUs
                    .Where(eu => eu.SubmitDate.HasValue)
                    .OrderByDescending(eu => eu.SubmitDate)
                    .Take(10)
                    .ToList() // Debug: lấy về list trước
                    .Select(eu => new
                    {
                        fullName = eu.FullName ?? "Unknown",
                        exerName = eu.ExerName ?? "Unknown",
                        submitDate = eu.SubmitDate.Value,
                        timeAgo = GetTimeAgo(eu.SubmitDate.Value)
                    })
                    .ToList();

                Console.WriteLine($"Recent activities count: {recentActivities.Count}");
                foreach (var activity in recentActivities)
                {
                    Console.WriteLine($"Activity: {activity.fullName} - {activity.exerName} ({activity.timeAgo})");
                }

                var result = new
                {
                    chartData = dailyStats,
                    recentActivities = recentActivities
                };

                Console.WriteLine("=== Final Result ===");
                Console.WriteLine($"ChartData count: {dailyStats.Count}");
                Console.WriteLine($"RecentActivities count: {recentActivities.Count}");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDashboardData: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // Helper method để tính thời gian đã trôi qua
        private string GetTimeAgo(DateTime submitDate)
        {
            var timeSpan = DateTime.Now - submitDate;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} min";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hr";
            else if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day";
            else
                return submitDate.ToString("MMM dd");
        }

        public IActionResult Accept(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            var e = _context.vEUs.Find(id);
            if (e == null)
                return NotFound();
            return View(e);
        }
        [HttpPost]
        public IActionResult Accept(tblEU exer)
        {
            if (ModelState.IsValid)
            {
                _context.EUs.Update(exer);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(exer);
        }

        public IActionResult Refuse(int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            var e = _context.vEUs.Find(id);
            if (e == null)
                return NotFound();
            return View(e);
        }
        [HttpPost]
        public IActionResult Refuse(int id)
        {
            var delExer = _context.EUs.Find(id);
            if (delExer == null)
                return NotFound();
            _context.EUs.Remove(delExer);
            _context.SaveChanges();
            return RedirectToAction("Index");
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

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", (eu.FilePath ?? "").TrimStart('/'));
                var sentences = Functions.ExtractSentencesFromPdf(filePath);
                var fullText = string.Join(" ", sentences);

                // Kiểm tra độ dài văn bản
                if (string.IsNullOrWhiteSpace(fullText))
                {
                    ViewBag.Error = "Không thể trích xuất nội dung từ file PDF.";
                    return View("Summary", eu);
                }

                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    ViewBag.Error = "Chưa cấu hình API key cho Gemini.";
                    return View("Summary", eu);
                }

                // Tạo tóm tắt với nhiều thông tin hơn
                var summaryResult = await Functions.SummarizeWithGemini(fullText, apiKey);

                // Thống kê văn bản
                var wordCount = fullText.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                var sentenceCount = sentences.Count;
                var paragraphCount = fullText.Split(new string[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length;

                ViewBag.Original = fullText;
                ViewBag.Summary = summaryResult.Summary;
                ViewBag.KeyPoints = summaryResult.KeyPoints;
                ViewBag.MainIdea = summaryResult.MainIdea;
                ViewBag.WordCount = wordCount;
                ViewBag.SentenceCount = sentenceCount;
                ViewBag.ParagraphCount = paragraphCount;
                ViewBag.SummaryWordCount = summaryResult.Summary?.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
                ViewBag.CompressionRatio = wordCount > 0 ? (double)ViewBag.SummaryWordCount / wordCount * 100 : 0;

                return View("Summary", eu);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Có lỗi xảy ra khi tóm tắt: {ex.Message}";
                return View("Summary", eu);
            }
        }
        [HttpPost]
        public async Task<IActionResult> CheckPlagiarism(int EUID)
        {
            var eu = _context.vEUs.Find(EUID);
            if (eu == null) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", (eu.FilePath ?? "").TrimStart('/'));
            var sentences = Functions.ExtractSentencesFromPdf(filePath);

            string apiKey = _configuration["GoogleSearch:ApiKey"]!;
            string cx = _configuration["GoogleSearch:CX"]!;

            var results = new List<(string sentence, bool isPlagiarized, int matchCount, List<string> urls)>();

            foreach (var sentence in sentences)
            {
                // Chỉ kiểm tra những câu có độ dài hợp lý (tránh câu quá ngắn)
                if (sentence.Length < 20)
                {
                    results.Add((sentence, false, 0, new List<string>()));
                    continue;
                }

                try
                {
                    var (matchCount, urls) = await Functions.CheckPlagiarismGoogle(sentence, apiKey, cx);
                    results.Add((sentence, matchCount > 0, matchCount, urls));

                    // Thêm delay để tránh rate limiting
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking sentence: {ex.Message}");
                    results.Add((sentence, false, 0, new List<string>()));
                }
            }

            ViewBag.Results = results;
            ViewBag.OriginalSentences = sentences;

            int plagiarizedCount = results.Count(r => r.isPlagiarized);
            float percentage = (results.Count > 0) ? (plagiarizedCount * 100f / results.Count) : 0f;
            ViewBag.PlagiarismRate = percentage;
            ViewBag.TotalSentences = results.Count;
            ViewBag.PlagiarizedSentences = plagiarizedCount;

            return View("Plagiarism", eu);
        }

        public async Task<IActionResult> Duplicate(int EUID)
        {
            var currentSubmission = _context.vEUs.Find(EUID);
            if (currentSubmission == null) return NotFound();

            try
            {
                // Lấy tất cả bài nộp cùng bài tập (EID) ngoại trừ bài hiện tại
                var otherSubmissions = _context.vEUs
                    .Where(x => x.EID == currentSubmission.EID && x.EUID != EUID && x.IsAcepted == true)
                    .ToList();

                var result = new DocumentComparisonResult
                {
                    CurrentEUID = EUID,
                    CurrentUserName = currentSubmission.FullName ?? "",
                    EID = currentSubmission.EID,
                    ExerciseName = currentSubmission.ExerName ?? "",
                    TotalSubmissions = otherSubmissions.Count + 1,
                    IsOnlySubmission = otherSubmissions.Count == 0
                };

                if (result.IsOnlySubmission)
                {
                    ViewBag.Message = "Hiện tại đây là bài duy nhất của bài tập này.";
                    return View("Duplicate", result);
                }

                // Trích xuất nội dung văn bản từ file hiện tại
                var currentFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    (currentSubmission.FilePath ?? "").TrimStart('/'));
                var currentText = ExtractTextFromFile(currentFilePath);

                if (string.IsNullOrWhiteSpace(currentText))
                {
                    ViewBag.Error = "Không thể trích xuất nội dung từ file hiện tại.";
                    return View("Duplicate", result);
                }

                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    ViewBag.Error = "Chưa cấu hình API key cho Gemini.";
                    return View("Duplicate", result);
                }

                // So sánh với từng bài khác
                var comparisons = new List<DuplicateResult>();

                foreach (var submission in otherSubmissions)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                        (submission.FilePath ?? "").TrimStart('/'));
                    var compareText = ExtractTextFromFile(filePath);

                    if (!string.IsNullOrWhiteSpace(compareText))
                    {
                        var duplicateCheck = await CheckDuplicateWithGemini(currentText, compareText, apiKey);

                        comparisons.Add(new DuplicateResult
                        {
                            EUID = submission.EUID,
                            FullName = submission.FullName ?? "",
                            SimilarityPercentage = duplicateCheck.SimilarityScore,
                            SimilarityLevel = GetSimilarityLevel(duplicateCheck.SimilarityScore),
                            SimilarSentences = duplicateCheck.SimilarSections
                        });
                    }
                }

                // Sắp xếp theo tỷ lệ trùng lặp giảm dần
                result.Comparisons = comparisons.OrderByDescending(x => x.SimilarityPercentage).ToList();

                return View("Duplicate", result);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Có lỗi xảy ra: {ex.Message}";
                return View("Duplicate", new DocumentComparisonResult
                {
                    CurrentEUID = EUID,
                    CurrentUserName = currentSubmission?.FullName ?? "",
                    IsOnlySubmission = true
                });
            }
        }

        private string ExtractTextFromFile(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return "";

                var extension = Path.GetExtension(filePath).ToLower();
                switch (extension)
                {
                    case ".pdf":
                        var sentences = Functions.ExtractSentencesFromPdf(filePath);
                        return string.Join(" ", sentences);
                    case ".txt":
                        return System.IO.File.ReadAllText(filePath);
                    case ".doc":
                    case ".docx":
                        // Implement Word document extraction if needed
                        return "";
                    default:
                        return "";
                }
            }
            catch
            {
                return "";
            }
        }

        public static async Task<DuplicateCheckResponse> CheckDuplicateWithGemini(string text1, string text2, string apiKey)
        {
            using (var client = new HttpClient())
            {
                var prompt = $@"Bạn là một chuyên gia phân tích văn bản. Hãy so sánh hai văn bản sau và đánh giá mức độ tương đồng.

Trả về kết quả theo định dạng JSON chính xác sau:
{{
  ""similarity_score"": 85.5,
  ""analysis"": ""Phân tích chi tiết về mức độ tương đồng"",
  ""similar_sections"": [
    ""Đoạn văn tương đồng 1"",
    ""Đoạn văn tương đồng 2""
  ]
}}

Yêu cầu:
- similarity_score: số thực từ 0-100 (0 = hoàn toàn khác biệt, 100 = giống hệt nhau)
- analysis: phân tích ngắn gọn về mức độ tương đồng
- similar_sections: tối đa 5 đoạn văn tương đồng nhất

CHỈ TRẢ VỀ JSON, KHÔNG CÓ TEXT KHÁC.

Văn bản 1:
{text1.Substring(0, Math.Min(text1.Length, 2000))}

Văn bản 2:
{text2.Substring(0, Math.Min(text2.Length, 2000))}";

                var requestBody = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        topK = 20,
                        topP = 0.8,
                        maxOutputTokens = 1024
                    }
                };

                try
                {
                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                        $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}",
                        content
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        return new DuplicateCheckResponse
                        {
                            SimilarityScore = 0,
                            Analysis = "Không thể kiểm tra do lỗi API",
                            SimilarSections = new List<string>()
                        };
                    }

                    var result = await response.Content.ReadAsStringAsync();
                    var parsed = JsonDocument.Parse(result);

                    var output = parsed.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return ParseDuplicateResponse(output ?? "");
                }
                catch (Exception ex)
                {
                    return new DuplicateCheckResponse
                    {
                        SimilarityScore = 0,
                        Analysis = $"Lỗi: {ex.Message}",
                        SimilarSections = new List<string>()
                    };
                }
            }
        }

        private static DuplicateCheckResponse ParseDuplicateResponse(string output)
        {
            try
            {
                // Làm sạch output
                var cleanOutput = output.Trim();
                if (cleanOutput.StartsWith("```json"))
                    cleanOutput = cleanOutput.Substring(7);
                if (cleanOutput.StartsWith("```"))
                    cleanOutput = cleanOutput.Substring(3);
                if (cleanOutput.EndsWith("```"))
                    cleanOutput = cleanOutput.Substring(0, cleanOutput.Length - 3);

                cleanOutput = cleanOutput.Trim();

                // Tìm JSON object
                var startIndex = cleanOutput.IndexOf('{');
                var endIndex = cleanOutput.LastIndexOf('}');

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    cleanOutput = cleanOutput.Substring(startIndex, endIndex - startIndex + 1);
                }

                var jsonDoc = JsonDocument.Parse(cleanOutput);
                var root = jsonDoc.RootElement;

                var similarSections = new List<string>();
                if (root.TryGetProperty("similar_sections", out var sectionsElement) &&
                    sectionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var section in sectionsElement.EnumerateArray())
                    {
                        var sectionText = section.GetString();
                        if (!string.IsNullOrWhiteSpace(sectionText))
                        {
                            similarSections.Add(sectionText.Trim());
                        }
                    }
                }

                return new DuplicateCheckResponse
                {
                    SimilarityScore = root.TryGetProperty("similarity_score", out var scoreElement)
                        ? scoreElement.GetDouble() : 0,
                    Analysis = root.TryGetProperty("analysis", out var analysisElement)
                        ? analysisElement.GetString() ?? "" : "",
                    SimilarSections = similarSections
                };
            }
            catch
            {
                // Fallback: thử phân tích đơn giản
                return new DuplicateCheckResponse
                {
                    SimilarityScore = 0,
                    Analysis = "Không thể phân tích kết quả so sánh",
                    SimilarSections = new List<string>()
                };
            }
        }

        private string GetSimilarityLevel(double percentage)
        {
            return percentage switch
            {
                >= 80 => "Rất cao",
                >= 60 => "Cao",
                >= 40 => "Trung bình",
                >= 20 => "Thấp",
                _ => "Rất thấp"
            };
        }

    }
}
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using final.Models;
using final.Utilities;
using System.Text.Json;
using System.Text.RegularExpressions;

// PDF processing libraries
#if HAS_ITEXT7
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
#endif

namespace final.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DataContext _context;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, DataContext context, IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        if (!Functions.IsLogin())
        {
            return RedirectToAction("Index", "Login", new { area = "Admin" });
        }
        return View();
    }

    public IActionResult Logout()
    {
        Functions._UserId = 0;
        Functions._UserName = string.Empty;
        Functions._Email = string.Empty;
        Functions._Message = string.Empty;
        Functions._FullName = string.Empty;
        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }

    [HttpGet]
    public IActionResult Update(int? id)
    {
        if (id == null || id == 0)
            return NotFound();

        var user = _context.Userss.Find(id);
        if (user == null)
            return NotFound();

        // Kiểm tra quyền truy cập - chỉ cho phép user chỉnh sửa thông tin của chính họ
        // Giả sử bạn có cách lấy current user ID
        if (user.UserId != Functions._UserId) // hoặc cách lấy current user ID khác
        {
            return Forbid();
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(tblUsers users, IFormFile imageFile)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Lấy thông tin user hiện tại từ database
                var existingUser = _context.Userss.Find(users.UserId);
                if (existingUser == null)
                    return NotFound();

                // Kiểm tra quyền truy cập
                if (existingUser.UserId != Functions._UserId)
                {
                    return Forbid();
                }

                // Cập nhật thông tin
                existingUser.FullName = users.FullName;
                existingUser.Email = users.Email;
                existingUser.T1 = users.T1;
                existingUser.T2 = users.T2;
                existingUser.T3 = users.T3;
                existingUser.T4 = users.T4;

                // Cập nhật password nếu có nhập mới
                if (!string.IsNullOrEmpty(users.Passwords))
                {
                    existingUser.Passwords = users.Passwords; // Nên hash password trước khi lưu
                }

                // Xử lý upload ảnh mới
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Path.GetFileName(imageFile.FileName);
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "img");

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Xóa ảnh cũ nếu không phải ảnh mặc định
                    if (!string.IsNullOrEmpty(existingUser.Images) && existingUser.Images != "default_avt.jpg")
                    {
                        var oldImagePath = Path.Combine(uploadPath, existingUser.Images);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    existingUser.Images = uniqueFileName;
                }

                _context.Userss.Update(existingUser);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Information updated successfully!";
                return RedirectToAction("Index"); // hoặc redirect về trang profile
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating information.";
                ModelState.AddModelError("", "Unable to save changes.");
            }
        }

        return View(users);
    }

    [Route("/Home/Details/{id:int}")]
    public IActionResult Details(int? id)
    {
        if (id == null)
            return NotFound();

        var exer = _context.Exers.FirstOrDefault(e => (e.ExerId == id));
        if (exer == null)
            return NotFound();

        // Check if user has already submitted
        var currentUserId = Functions._UserId;
        var existingSubmission = _context.EUs.FirstOrDefault(eu => eu.EID == id && eu.UID == currentUserId);

        ViewBag.ExistingSubmission = existingSubmission;
        ViewBag.IsDeadlinePassed = exer.Deadline.HasValue && DateTime.Now > exer.Deadline.Value;

        return View(exer);
    }

    [HttpGet]
    public IActionResult Upload(int exerId)
    {
        return View(exerId);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(int exerId, IFormFile pdfFile)
    {
        var exer = _context.Exers.FirstOrDefault(e => e.ExerId == exerId);
        if (exer == null)
            return NotFound();

        // Check deadline
        if (exer.Deadline.HasValue && DateTime.Now > exer.Deadline.Value)
        {
            TempData["UploadError"] = "Đã quá hạn nộp bài!";
            return RedirectToAction("Details", new { id = exerId });
        }

        if (pdfFile != null && pdfFile.Length > 0)
        {
            var fileExt = Path.GetExtension(pdfFile.FileName).ToLower();
            if (fileExt != ".pdf")
            {
                TempData["UploadError"] = "Chỉ chấp nhận file PDF.";
                return RedirectToAction("Details", new { id = exerId });
            }

            try
            {
                // Extract PDF content
                var pdfContent = ExtractPdfContent(pdfFile);

                // Perform plagiarism check
                var plagiarismResult = await CheckPlagiarism(pdfContent);

                if (plagiarismResult.PlagiarismPercentage > 60)
                {
                    TempData["PlagiarismResult"] = JsonSerializer.Serialize(plagiarismResult);
                    TempData["UploadError"] = $"Tỷ lệ đạo văn quá cao ({plagiarismResult.PlagiarismPercentage:F1}%)! Hãy trung thực trong việc làm bài.";
                    return RedirectToAction("Details", new { id = exerId });
                }

                // Save file
                var fileName = $"exer_{exerId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var relativePath = $"/uploads/{fileName}";
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                var currentUserId = Functions._UserId;

                // Check if user already has a submission
                var existingSubmission = _context.EUs.FirstOrDefault(eu => eu.EID == exerId && eu.UID == currentUserId);

                if (existingSubmission != null)
                {
                    // Update existing submission
                    existingSubmission.FilePath = relativePath;
                    existingSubmission.SubmitDate = DateTime.Now;
                    existingSubmission.IsAcepted = false; // Reset acceptance status
                }
                else
                {
                    // Create new submission
                    var eu = new tblEU
                    {
                        UID = currentUserId,
                        EID = exerId,
                        FilePath = relativePath,
                        IsAcepted = false,
                        SubmitDate = DateTime.Now
                    };
                    _context.EUs.Add(eu);
                }

                await _context.SaveChangesAsync();

                TempData["PlagiarismResult"] = JsonSerializer.Serialize(plagiarismResult);
                TempData["UploadSuccess"] = $"Nộp bài thành công! Tỷ lệ đạo văn: {plagiarismResult.PlagiarismPercentage:F1}%";
                return RedirectToAction("Details", new { id = exerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDF upload");
                TempData["UploadError"] = "Có lỗi xảy ra khi xử lý file PDF.";
                return RedirectToAction("Details", new { id = exerId });
            }
        }

        TempData["UploadError"] = "Chưa chọn file hoặc file không hợp lệ!";
        return RedirectToAction("Details", new { id = exerId });
    }

    private string ExtractPdfContent(IFormFile pdfFile)
    {
        try
        {
#if HAS_ITEXT7
            using var stream = pdfFile.OpenReadStream();
            using var pdfReader = new PdfReader(stream);
            using var pdfDocument = new PdfDocument(pdfReader);

            var content = new List<string>();

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var pageContent = PdfTextExtractor.GetTextFromPage(page);

                if (!string.IsNullOrWhiteSpace(pageContent))
                {
                    content.Add(pageContent.Trim());
                }
            }

            return string.Join("\n\n", content);
#else
            // Fallback implementation without iText7
            using var reader = new StreamReader(pdfFile.OpenReadStream());
            return "PDF content extraction requires iText7 library. Please install: dotnet add package itext7";
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting PDF content");
            return "Error extracting PDF content: " + ex.Message;
        }
    }

    private async Task<PlagiarismResult> CheckPlagiarism(string content)
    {
        var apiKey = _configuration["GoogleSearch:ApiKey"];
        var cx = _configuration["GoogleSearch:Cx"];

        var result = new PlagiarismResult
        {
            OriginalContent = content,
            PlagiarizedSentences = new List<PlagiarizedSentence>()
        };

        // Split content into paragraphs and sentences
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var allSentences = new List<(string sentence, int paragraphIndex, int sentenceIndex)>();

        for (int pIndex = 0; pIndex < paragraphs.Length; pIndex++)
        {
            var sentences = SplitIntoSentences(paragraphs[pIndex]);
            for (int sIndex = 0; sIndex < sentences.Count; sIndex++)
            {
                if (!string.IsNullOrWhiteSpace(sentences[sIndex]) && sentences[sIndex].Length > 10)
                {
                    allSentences.Add((sentences[sIndex].Trim(), pIndex, sIndex));
                }
            }
        }

        int totalSentences = allSentences.Count;
        int plagiarizedCount = 0;

        foreach (var (sentence, paragraphIndex, sentenceIndex) in allSentences)
        {
            try
            {
                var (totalResults, links) = await CheckPlagiarismGoogle(sentence, apiKey, cx);

                if (totalResults > 0)
                {
                    plagiarizedCount++;

                    var plagiarizedSentence = new PlagiarizedSentence
                    {
                        Sentence = sentence,
                        ParagraphIndex = paragraphIndex,
                        SentenceIndex = sentenceIndex,
                        SourceCount = Math.Min(totalResults, 10), // Cap at 10 for color mapping
                        Sources = links.Take(5).ToList() // Show top 5 sources
                    };

                    result.PlagiarizedSentences.Add(plagiarizedSentence);
                }

                // Add small delay to avoid rate limiting
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking plagiarism for sentence: {sentence.Substring(0, Math.Min(50, sentence.Length))}...");
            }
        }

        result.PlagiarismPercentage = totalSentences > 0 ? (double)plagiarizedCount / totalSentences * 100 : 0;

        return result;
    }

    private List<string> SplitIntoSentences(string paragraph)
    {
        // Simple sentence splitting - can be improved with more sophisticated NLP
        var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return sentences;
    }

    public static async Task<(int totalResults, List<string> links)> CheckPlagiarismGoogle(string paragraph, string apiKey, string cx)
    {
        using var client = new HttpClient();
        var query = Uri.EscapeDataString($"\"{paragraph}\""); // tìm chính xác đoạn văn

        var url = $"https://www.googleapis.com/customsearch/v1?q={query}&key={apiKey}&cx={cx}";
        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        var parsed = JsonDocument.Parse(json);
        var totalStr = parsed.RootElement
            .GetProperty("searchInformation")
            .GetProperty("totalResults")
            .GetString();

        int.TryParse(totalStr, out int total);

        var links = new List<string>();
        if (parsed.RootElement.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("link", out var link))
                {
                    links.Add(link.GetString() ?? "");
                }
            }
        }

        return (total, links);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

// Supporting classes
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
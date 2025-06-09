using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace final.Utilities
{
    public class Functions
    {
        public static int _UserId = 0;
        public static string _UserName = string.Empty;
        public static string _FullName = string.Empty;
        public static string _Email = string.Empty;
        public static string _Message = string.Empty;
        public static string TitleSlugGeneration(string type, string? title, long id)
        {
            return type + "-" + SlugGenerator.SlugGenerator.GenerateSlug(title) + "-" + id.ToString() + ".html";
        }

        public static string MD5Hash(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(text);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder strBuilder = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    strBuilder.Append(b.ToString("x2"));
                }
                return strBuilder.ToString();
            }
        }

        public static string MD5Password(string? text)
        {
            string str = MD5Hash(text ?? string.Empty);
            for (int i = 0; i <= 5; i++)
            {
                str = MD5Hash(str + str);
            }
            return str;
        }
        public static bool IsLogin()
        {
            if ((Functions._UserId <= 0) || string.IsNullOrEmpty(Functions._UserName) || string.IsNullOrEmpty(Functions._Email))
            {
                return false;
            }
            return true;
        }

        public static async Task<SummaryResult> SummarizeWithGemini(string inputText, string apiKey)
        {
            using (var client = new HttpClient())
            {
                // Tạo prompt rõ ràng hơn với yêu cầu format cụ thể
                var prompt = @"Bạn là một trợ lý AI chuyên tóm tắt văn bản tiếng Việt. Hãy phân tích văn bản sau và trả về CHÍNH XÁC theo định dạng JSON sau đây:

{
  ""main_idea"": ""Ý tưởng chính của văn bản (1 câu ngắn gọn)"",
  ""summary"": ""Tóm tắt một cách ngắn gọn các chi tiết của văn bản, đưa các những nội dung chính nhất nhưng rõ nghĩa từng câu."",
  ""key_points"": [
    ""Điểm chính thứ nhất"",
    ""Điểm chính thứ hai"",
    ""Điểm chính thứ ba"",
    ""Điểm chính thứ tư (nếu có)""
  ]
}

CHỈ TRẢ VỀ JSON, KHÔNG CÓ BẤT KỲ TEXT NÀO KHÁC.

Văn bản cần phân tích:
" + inputText;

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
                        temperature = 0.1, // Giảm temperature để có kết quả ổn định hơn
                        topK = 20,
                        topP = 0.8,
                        maxOutputTokens = 2048
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(
                        $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}",
                        content
                    );

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        return new SummaryResult
                        {
                            Summary = $"Lỗi API: {response.StatusCode}",
                            MainIdea = "Không thể tạo tóm tắt do lỗi API",
                            KeyPoints = new List<string> { $"Chi tiết lỗi: {errorContent}" }
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

                    if (string.IsNullOrEmpty(output))
                    {
                        return new SummaryResult
                        {
                            Summary = "Không có phản hồi từ AI",
                            MainIdea = "Không xác định",
                            KeyPoints = new List<string> { "Không có dữ liệu trả về" }
                        };
                    }

                    // Làm sạch output và cố gắng parse JSON
                    return ParseGeminiResponse(output);
                }
                catch (HttpRequestException httpEx)
                {
                    return new SummaryResult
                    {
                        Summary = "Lỗi kết nối mạng khi gọi API Gemini",
                        MainIdea = "Không thể kết nối đến dịch vụ AI",
                        KeyPoints = new List<string> { $"Chi tiết: {httpEx.Message}" }
                    };
                }
                catch (Exception ex)
                {
                    return new SummaryResult
                    {
                        Summary = "Có lỗi không xác định xảy ra",
                        MainIdea = "Không thể tạo tóm tắt",
                        KeyPoints = new List<string> { $"Lỗi: {ex.Message}" }
                    };
                }
            }
        }

        private static SummaryResult ParseGeminiResponse(string output)
        {
            try
            {
                // Loại bỏ các ký tự không cần thiết
                var cleanOutput = output.Trim();

                // Loại bỏ markdown code blocks nếu có
                if (cleanOutput.StartsWith("```json"))
                {
                    cleanOutput = cleanOutput.Substring(7);
                }
                if (cleanOutput.StartsWith("```"))
                {
                    cleanOutput = cleanOutput.Substring(3);
                }
                if (cleanOutput.EndsWith("```"))
                {
                    cleanOutput = cleanOutput.Substring(0, cleanOutput.Length - 3);
                }

                cleanOutput = cleanOutput.Trim();

                // Tìm JSON object trong response
                var startIndex = cleanOutput.IndexOf('{');
                var endIndex = cleanOutput.LastIndexOf('}');

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    cleanOutput = cleanOutput.Substring(startIndex, endIndex - startIndex + 1);
                }

                // Parse JSON
                var summaryJson = JsonDocument.Parse(cleanOutput);
                var root = summaryJson.RootElement;

                // Extract main idea
                var mainIdea = "Không xác định";
                if (root.TryGetProperty("main_idea", out var mainIdeaElement))
                {
                    mainIdea = mainIdeaElement.GetString() ?? mainIdea;
                }

                // Extract summary
                var summary = "Không có tóm tắt";
                if (root.TryGetProperty("summary", out var summaryElement))
                {
                    summary = summaryElement.GetString() ?? summary;
                }

                // Extract key points
                var keyPoints = new List<string>();
                if (root.TryGetProperty("key_points", out var keyPointsElement) && keyPointsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var point in keyPointsElement.EnumerateArray())
                    {
                        var pointText = point.GetString();
                        if (!string.IsNullOrWhiteSpace(pointText))
                        {
                            keyPoints.Add(pointText.Trim());
                        }
                    }
                }

                // Đảm bảo có ít nhất một số key points
                if (keyPoints.Count == 0)
                {
                    keyPoints.Add("Không thể xác định các điểm chính");
                }

                return new SummaryResult
                {
                    MainIdea = mainIdea,
                    Summary = summary,
                    KeyPoints = keyPoints
                };
            }
            catch (JsonException jsonEx)
            {
                // Nếu không parse được JSON, thử phân tích văn bản thô
                return FallbackTextAnalysis(output, jsonEx.Message);
            }
            catch (Exception ex)
            {
                return new SummaryResult
                {
                    Summary = $"Lỗi khi phân tích phản hồi: {ex.Message}",
                    MainIdea = "Không thể phân tích",
                    KeyPoints = new List<string> { "Có lỗi trong quá trình xử lý phản hồi từ AI" }
                };
            }
        }

        private static SummaryResult FallbackTextAnalysis(string text, string jsonError)
        {
            try
            {
                // Nếu JSON parsing thất bại, thử phân tích text thô
                var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(l => l.Trim())
                                .Where(l => !string.IsNullOrEmpty(l))
                                .ToList();

                var mainIdea = "Xem nội dung tóm tắt";
                var summary = text;
                var keyPoints = new List<string>();

                // Thử tìm các pattern phổ biến
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i].ToLower();

                    // Tìm main idea
                    if (line.Contains("ý tưởng chính") || line.Contains("main idea") || line.Contains("chủ đề"))
                    {
                        if (i + 1 < lines.Count)
                        {
                            mainIdea = lines[i + 1];
                        }
                    }

                    // Tìm key points
                    if (line.Contains("điểm chính") || line.Contains("key point") ||
                        line.StartsWith("- ") || line.StartsWith("• ") ||
                        System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d+\."))
                    {
                        var point = lines[i].TrimStart('-', '•', ' ');
                        point = System.Text.RegularExpressions.Regex.Replace(point, @"^\d+\.\s*", "");
                        if (!string.IsNullOrWhiteSpace(point) && point.Length > 10)
                        {
                            keyPoints.Add(point);
                        }
                    }
                }

                // Nếu không tìm được key points, tạo từ summary
                if (keyPoints.Count == 0)
                {
                    var sentences = summary.Split('.', '!', '?')
                                          .Select(s => s.Trim())
                                          .Where(s => s.Length > 20)
                                          .Take(4)
                                          .ToList();

                    keyPoints.AddRange(sentences);
                }

                // Đảm bảo có ít nhất một key point
                if (keyPoints.Count == 0)
                {
                    keyPoints.Add("Không thể phân tích các điểm chính từ nội dung");
                }

                return new SummaryResult
                {
                    MainIdea = mainIdea,
                    Summary = summary,
                    KeyPoints = keyPoints
                };
            }
            catch
            {
                // Fallback cuối cùng
                return new SummaryResult
                {
                    Summary = text,
                    MainIdea = "Không thể phân tích ý tưởng chính",
                    KeyPoints = new List<string> { $"Lỗi JSON: {jsonError}", "Không thể phân tích cấu trúc phản hồi" }
                };
            }
        }

        // Class kết quả (giữ nguyên)
        public class SummaryResult
        {
            public string MainIdea { get; set; } = "";
            public string Summary { get; set; } = "";
            public List<string> KeyPoints { get; set; } = new List<string>();
        }
        public static List<string> ExtractSentencesFromPdf(string filePath)
        {
            var sentences = new List<string>();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                var fullText = string.Join(" ", document.GetPages().Select(p => p.Text));

                // Làm sạch text trước khi tách câu
                fullText = CleanText(fullText);

                // Tách câu bằng regex phức tạp hơn để xử lý các trường hợp đặc biệt
                var rawSentences = Regex.Split(fullText,
                    @"(?<=[.!?])\s+(?=[A-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠƯĂÂÊÔƠƯẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼẾỀỂỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪỬỮỰỲỴÝỶỸ])",
                    RegexOptions.IgnoreCase);

                foreach (var s in rawSentences)
                {
                    var cleanSentence = s.Trim();
                    // Chỉ thêm những câu có ý nghĩa (không phải chỉ số, ký tự đặc biệt)
                    if (!string.IsNullOrWhiteSpace(cleanSentence) &&
                        cleanSentence.Length > 10 &&
                        ContainsLetters(cleanSentence))
                    {
                        sentences.Add(cleanSentence);
                    }
                }
            }
            return sentences;
        }

        private static string CleanText(string text)
        {
            // Loại bỏ các ký tự không cần thiết, xuống dòng thừa
            text = Regex.Replace(text, @"\s+", " "); // Thay thế nhiều khoảng trắng thành 1
            text = Regex.Replace(text, @"\n+", " "); // Thay thế xuống dòng thành khoảng trắng
            text = text.Trim();
            return text;
        }

        private static bool ContainsLetters(string text)
        {
            // Kiểm tra xem câu có chứa chữ cái không (tránh câu chỉ có số hoặc ký tự đặc biệt)
            return Regex.IsMatch(text, @"[a-zA-Zàáâãèéêìíòóôõùúăđĩũơưăâêôơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵýỷỹ]");
        }

        public static async Task<(int totalResults, List<string> links)> CheckPlagiarismGoogle(string sentence, string apiKey, string cx)
        {
            using var client = new HttpClient();

            // Tối ưu query: chỉ lấy phần quan trọng của câu nếu câu quá dài
            var queryText = sentence.Length > 100 ? sentence.Substring(0, 100) + "..." : sentence;
            var query = Uri.EscapeDataString($"\"{queryText}\"");

            var url = $"https://www.googleapis.com/customsearch/v1?q={query}&key={apiKey}&cx={cx}&num=5";

            try
            {
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API Error: {response.StatusCode}");
                    return (0, new List<string>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var parsed = JsonDocument.Parse(json);

                // Kiểm tra xem có searchInformation không
                if (!parsed.RootElement.TryGetProperty("searchInformation", out var searchInfo))
                {
                    return (0, new List<string>());
                }

                var totalStr = searchInfo.GetProperty("totalResults").GetString();
                int.TryParse(totalStr, out int total);

                var links = new List<string>();
                if (parsed.RootElement.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("link", out var link))
                        {
                            var linkStr = link.GetString();
                            if (!string.IsNullOrEmpty(linkStr))
                            {
                                links.Add(linkStr);
                            }
                        }
                    }
                }

                return (total, links);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckPlagiarismGoogle: {ex.Message}");
                return (0, new List<string>());
            }
        }


    }
}
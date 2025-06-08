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
            if ((Functions._UserId <=0) || string.IsNullOrEmpty(Functions._UserName) || string.IsNullOrEmpty(Functions._Email))
            {
                return false;
            }
            return true;
        }
        public static List<string> ExtractSentencesFromPdf(string filePath)
        {
            var sentences = new List<string>();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                var fullText = string.Join(" ", document.GetPages().Select(p => p.Text));
                // Tách câu bằng dấu chấm (hoặc dấu kết thúc câu khác)
                var rawSentences = Regex.Split(fullText, @"(?<=[.!?])\s+");
                foreach (var s in rawSentences)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                        sentences.Add(s.Trim());
                }
            }
            return sentences;
        }
        public static async Task<string> SummarizeWithGemini(string inputText, string apiKey)
        {
            using (var client = new HttpClient())
            {
                var prompt = "Hãy tóm tắt đoạn văn sau một cách ngắn gọn, rõ ràng và giữ lại các ý chính nhất:\n\n";
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt + inputText }
                            }
                        }
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}",
                    content
                );

                if (!response.IsSuccessStatusCode) return $"Lỗi API: {response.StatusCode}";

                var result = await response.Content.ReadAsStringAsync();
                var parsed = JsonDocument.Parse(result);
                var output = parsed.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return output ?? "Không có nội dung trả về từ Gemini";
            }
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

        public static List<string> ExtractParagraphsFromPdf(string filePath)
        {
            var sentences = ExtractSentencesFromPdf(filePath);
            string rawText = string.Join(" ", sentences);

            return rawText.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 50)
                    .ToList();
        }


    }
}
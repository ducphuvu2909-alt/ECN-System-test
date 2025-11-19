using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebApp.AI
{
    /// <summary>
    /// Client gọi ChatGPT thông qua OpenAI API.
    /// Được cấu hình qua OpenAiOptions + HttpClient DI.
    /// </summary>
    public class OpenAiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<OpenAiClient> _logger;
        private readonly OpenAiOptions _opt;

        public OpenAiClient(
            HttpClient http,
            ILogger<OpenAiClient> logger,
            IOptions<OpenAiOptions> opt)
        {
            _http = http;
            _logger = logger;
            _opt = opt.Value;

            if (!string.IsNullOrWhiteSpace(_opt.ApiKey))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _opt.ApiKey);
            }

            if (!string.IsNullOrWhiteSpace(_opt.BaseUrl))
            {
                _http.BaseAddress = new Uri(_opt.BaseUrl);
            }
        }

        /// <summary>
        /// Gọi ChatGPT (Chat Completions API).
        /// System Prompt được truyền trực tiếp vào phần messages.
        /// </summary>
        public async Task<string> AskAsync(string systemPrompt, string userPrompt, int maxTokens = 600)
        {
            if (string.IsNullOrWhiteSpace(_opt.ApiKey))
                return "⚠ Chưa cấu hình OpenAI API Key.";

            var body = new
            {
                model = _opt.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = maxTokens
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage resp;

            try
            {
                resp = await _http.PostAsync("chat/completions", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi OpenAI.");
                return "⚠ Không thể kết nối OpenAI (network error).";
            }

            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI error {Status}: {Body}", resp.StatusCode, respText);
                return "⚠ OpenAI trả về lỗi, kiểm tra API Key / Model.";
            }

            try
            {
                using var doc = JsonDocument.Parse(respText);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi parse JSON OpenAI.");
                return "⚠ Không đọc được phản hồi từ OpenAI.";
            }
        }
    }
}

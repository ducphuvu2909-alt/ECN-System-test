using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApp.Data;

namespace WebApp.Services
{
    /// <summary>
    /// Lõi AI cho ECN Manager: hiểu câu hỏi, route nội bộ / ChatGPT, trả về câu trả lời đã xuống dòng.
    /// </summary>
    public class AiAdvisorService
    {
        private const string InternalFallbackPrefix =
            "• Em chưa tìm được câu trả lời chính xác trong knowledge nội bộ ECN.";

        private readonly ILogger<AiAdvisorService> _logger;
        private readonly HttpClient _http;
        private readonly AiSettings _aiSettings;
        private readonly OpenAiOptions _openAi;
        private readonly EmailService _emailService;
        private readonly EcnService _ecnService;
        private readonly DeptService _deptService;

        public AiAdvisorService(
            ILogger<AiAdvisorService> logger,
            HttpClient http,
            IOptions<AiSettings> aiSettings,
            IOptions<OpenAiOptions> openAi,
            EmailService emailService,
            EcnService ecnService,
            DeptService deptService)
        {
            _logger = logger;
            _http = http;
            _aiSettings = aiSettings.Value;
            _openAi = openAi.Value;
            _emailService = emailService;
            _ecnService = ecnService;
            _deptService = deptService;
        }

        /// <summary>
        /// Hàm chính được controller gọi.
        /// </summary>
        public async Task<string> AskAsync(string question, string? userName, string? userDept)
        {
            var q = (question ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(q))
                return "• Câu hỏi trống.\n• Anh/chị hãy nhập nội dung cụ thể hơn.";

            var lower = q.ToLowerInvariant();

            // 1) Nếu là câu hỏi ngoài hệ thống ECN (giá, thời tiết, người nổi tiếng...)
            if (IsExternalQuestion(lower))
            {
                if (!_aiSettings.EnableExternalLlm)
                {
                    return FormatMultiline(
                        "• Đây là hệ thống nội bộ của PSNV (ECN Manager)." +
                        "\n• AI chỉ hỗ trợ các câu hỏi liên quan đến ECN, WI, TNA, BOM và quy trình nội bộ." +
                        "\n• Admin có thể bật 'Cho phép ChatGPT' trong trang cấu hình nếu muốn hỏi ngoài phạm vi."
                    );
                }

                var ext = await AskOpenAiAsync(q);
                return FormatMultiline(ext);
            }

            // 2) Câu hỏi nội bộ ECN
            var internalAnswer = await AnswerInternalAsync(q, lower, userName, userDept);

            // Nếu nội bộ đã trả lời rõ ràng (không phải fallback) -> dùng nội bộ
            if (!internalAnswer.StartsWith(InternalFallbackPrefix, StringComparison.Ordinal))
            {
                return FormatMultiline(internalAnswer);
            }

            // Đến đây là: nội bộ không tìm được câu trả lời rõ ràng
            // Nếu không cho phép external LLM -> giữ nguyên fallback nội bộ
            if (!_aiSettings.EnableExternalLlm)
            {
                return FormatMultiline(internalAnswer);
            }

            // Cho phép dùng ChatGPT để thử trả lời thêm (vẫn với system prompt ECN)
            var extInternal = await AskOpenAiAsync(q);
            return FormatMultiline(extInternal);
        }

        /// <summary>
        /// Phân loại câu hỏi có phải ngoài hệ thống ECN hay không.
        /// </summary>
        private bool IsExternalQuestion(string lower)
        {
            string[] priceKeywords = { "giá btc", "btc giá", "bitcoin", "giá vàng", "giá usd", "stock", "chứng khoán" };
            string[] weatherKeywords = { "thời tiết", "weather", "nhiệt độ", "mưa", "trời hôm nay" };
            string[] celebrityKeywords = { "elon", "elon musk", "bill gates", "taylor swift" };

            bool Any(string[] arr) => arr.Any(k => lower.Contains(k));

            return Any(priceKeywords) || Any(weatherKeywords) || Any(celebrityKeywords);
        }

        /// <summary>
        /// Trả lời các câu hỏi nội bộ ECN (FAQ + data thật).
        /// Anh có thể bổ sung thêm rule/phòng ban trong hàm này.
        /// </summary>
        private async Task<string> AnswerInternalAsync(string raw, string lower, string? userName, string? userDept)
        {
            // --- 1. Định nghĩa ECN
            if (lower.Contains("ecn là gì") || lower == "ecn" || lower.StartsWith("ecn "))
            {
                return @"ECN (Engineering Change Notice) là **thủ tục quản trị thay đổi kỹ thuật** của nhà máy.

• Mục đích: Chuẩn hoá luồng thay đổi thiết kế/quy trình/vật tư, giảm rủi ro phát hành sai.
• Luồng chuẩn: Đề xuất → Đánh giá tác động → Phê duyệt → Triển khai → Xác nhận & lưu vết.
• Đối tượng: Model, BOM, WI, QP, tiêu chuẩn kiểm tra, dụng cụ, thiết bị, layout, thời gian thao tác...
• Hệ thống ECN Manager: giúp tự động hoá đọc TNA, mapping BOM/WI, cảnh báo quá hạn, audit và đào tạo.";
            }

            // --- 2. Cấu trúc hệ thống ECN
            if (lower.Contains("hệ thống ecn") || lower.Contains("cấu trúc hệ thống ecn"))
            {
                return @"Cấu trúc hệ thống ECN Manager (tổng quan):

1) Cổng người dùng
   • Đăng nhập, phân quyền theo bộ phận (FE, PMS, QC, PUR, COS, SMT, DIP, FA, IT...).
   • Bảng điều khiển trạng thái ECN, heatmap quá hạn.

2) ECN Master
   • Nạp dữ liệu TNA/ECN từ file hoặc SAP.
   • Mapping Before/After, hiệu lực, Rank, model, WI, QP...

3) ECN Department
   • Mỗi phòng ban có tab view riêng: kế hoạch, actual date, chứng từ WI/QP, status.
   • AI đọc cross-department, phát hiện lệch, cảnh báo.

4) Module bổ trợ
   • WI Verify: so sánh WI với ECN Master.
   • SAP Ingest: đọc kế hoạch/valid từ SAP.
   • Notify: popup + email cảnh báo DUE_SOON / OVERDUE.

5) ECN AI Advisor
   • Trả lời câu hỏi & đề xuất hành động cho từng bộ phận dựa trên dữ liệu thật trong ECN Manager.";
            }

            // --- 3. Phòng FE
            if (lower == "fe" || lower.Contains("phòng fe") ||
                lower.Contains("factory engineering") || lower.Contains("kỹ thuật nhà máy"))
            {
                return @"Phòng FE (Factory Engineering) — **Phòng kỹ thuật nhà máy**.

FE là gì?
• Phụ trách hạ tầng kỹ thuật sản xuất, chuẩn hoá quy trình, triển khai sản phẩm mới và quản trị thay đổi (ECN).

4 nhóm chính:
• Electrical — Xử lý vấn đề về điện; phân tích lỗi phần điện; đánh giá/khắc phục EMC.
• Mechanical — Xử lý vấn đề cơ khí; phân tích lỗi cơ; đánh giá & phê duyệt linh kiện cơ khí; hỗ trợ R&D.
• Technical — Lập kế hoạch NPI, chạy thử/pilot; quản lý QP; quản lý WI; quản lý ECN nhà máy; Standard Time.
• Tooling — Thiết kế & gia công khuôn cho nhà máy; sửa chữa khuôn cho supplier & PSNV.

Liên quan ECN:
• FE đánh giá tác động kỹ thuật/thiết bị/khuôn, chuẩn hoá WI/QP, xác nhận năng lực dây chuyền trước khi triển khai.";
            }

            // TODO: anh có thể bổ sung PMS / QC / PUR / COS / SMT / DIP / FA / IT... ngay dưới đây.

            // --- 4. Ví dụ câu hỏi data: ECN quá hạn
            if (lower.Contains("ecn quá hạn") || lower.Contains("ecn overdue"))
            {
               // var stats = await _ecnService.GetOverdueSummaryAsync();  (tạm thời sửa lệnh này bằng đoạn lệnh phía dưới)
                // Tạm thời bỏ gọi GetOverdueSummaryAsync vì chưa implement trong EcnService
                var overdueSummary = "Chức năng thống kê Overdue hiện đang được cập nhật trong hệ thống ECN.";

// Nếu phía dưới có build prompt, anh thay dùng overdueSummary
// ví dụ:
                promptBuilder.AppendLine(overdueSummary);

                return $@"Báo cáo ECN quá hạn (tóm tắt):

• Tổng số ECN quá hạn: {stats.TotalOverdue}
• FE: {stats.FeOverdue}  | QC: {stats.QcOverdue}  | PMS: {stats.PmsOverdue}
• SMT: {stats.SmtOverdue} | DIP: {stats.DipOverdue} | FA: {stats.FaOverdue}

Đề xuất:
• Ưu tiên xử lý các ECN liên quan model đang có kế hoạch sản xuất gần nhất.
• Mở màn hình 'ECN Overdue' để xem chi tiết từng số ECN và phân công người phụ trách.";
            }

            // --- 99. Không khớp rule nào (fallback nội bộ)
            return $@"{InternalFallbackPrefix}
• Anh/chị có thể diễn đạt rõ hơn (ví dụ: 'ECN là gì', 'Quy trình FE trong ECN', 'ECN quá hạn bộ phận PMS'...).
• Nếu cần bật chế độ hỏi ngoài hệ thống (ChatGPT), hãy để Admin bật 'Allow external LLM'.";
        }

        /// <summary>
        /// Gọi ChatGPT / OpenAI khi cho phép hỏi ngoài hệ thống ECN hoặc fallback nội bộ.
        /// </summary>
        private async Task<string> AskOpenAiAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(_openAi.ApiKey))
                return "• Chưa cấu hình API key OpenAI.\n• Admin vui lòng nhập API key trong cấu hình server.";

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _openAi.ApiKey);

            var payload = new
            {
                model = _openAi.Model,
                messages = new[]
                {
                    new { role = "system", content =
                        "You are ECN Manager assistant for a factory. " +
                        "When user asks about ECN, WI, BOM, SAP, answer in Vietnamese with clear bullet points. " +
                        "If question is clearly general knowledge (weather, price, people...), answer normally but concise." },
                    new { role = "user", content = question }
                },
                max_tokens = _aiSettings.MaxAnswerTokens
            };

            var json = JsonSerializer.Serialize(payload);
            var resp = await _http.PostAsync(
                $"{_openAi.BaseUrl}/chat/completions",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI error: {Status} {Body}", resp.StatusCode, body);
                return "• Gọi OpenAI thất bại.\n• Anh/chị kiểm tra lại API key / model / kết nối mạng giúp em.";
            }

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? string.Empty;
        }

        /// <summary>
        /// Chuẩn hoá xuống dòng cho dễ đọc.
        /// </summary>
        private string FormatMultiline(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            var txt = raw.Replace("\r", string.Empty);

            if (!txt.Contains('\n'))
            {
                txt = txt.Replace("• ", "\n• ");
                txt = txt.Replace(". ", ".\n");
            }

            return txt.Trim();
        }
    }
}


namespace WebApp.Services
{
    /// <summary>
    /// Cấu hình AI nội bộ của hệ thống ECN Manager.
    /// Đọc từ appsettings.json → section "Ai".
    /// </summary>
    public class AiSettings
    {
        /// <summary>
        /// Cho phép dùng ChatGPT khi câu hỏi ngoài ECN?
        /// Nếu false → AI chỉ trả lời nội bộ.
        /// </summary>
        public bool EnableExternalLlm { get; set; } = false;

        /// <summary>
        /// Prompt hệ thống cho ChatGPT (đặt role).
        /// </summary>
        public string SystemPrompt { get; set; } =
            "Bạn là trợ lý AI nội bộ của hệ thống ECN Manager.";

        /// <summary>
        /// Max token AI được phép trả lời.
        /// </summary>
        public int MaxAnswerTokens { get; set; } = 600;
    }
}

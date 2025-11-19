namespace WebApp.Data
{
    /// <summary>
    /// Cấu hình tổng cho AI trong ECN Manager.
    /// </summary>
    public class AiSettings
    {
        /// <summary>
        /// Cho phép gọi LLM bên ngoài (ChatGPT). Nếu false, AI chỉ trả lời nội bộ ECN.
        /// </summary>
        public bool EnableExternalLlm { get; set; } = false;

        /// <summary>
        /// Ngôn ngữ trả lời mặc định (vi/en/ja...)
        /// </summary>
        public string DefaultLanguage { get; set; } = "vi";

        /// <summary>
        /// Giới hạn độ dài câu trả lời khi gọi OpenAI.
        /// </summary>
        public int MaxAnswerTokens { get; set; } = 800;
    }
}

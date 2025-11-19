namespace WebApp.AI
{
    /// <summary>
    /// Cấu hình cho OpenAI / ChatGPT.
    /// Đọc từ appsettings.json → section "OpenAI".
    /// </summary>
    public class OpenAiOptions
    {
        /// <summary>
        /// API Key của OpenAI (bắt buộc khi EnableExternalLlm = true).
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Base URL của OpenAI API.
        /// Thường mặc định: https://api.openai.com/v1
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";

        /// <summary>
        /// Model ChatGPT sẽ dùng.
        /// Ví dụ: gpt-4.1-mini, gpt-4o, gpt-4.1
        /// </summary>
        public string Model { get; set; } = "gpt-4.1-mini";
    }
}

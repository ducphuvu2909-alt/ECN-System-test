namespace WebApp.AI
{
    /// <summary>
    /// Chính sách cho AI: chỉ nội bộ hay cho phép dùng ChatGPT.
    /// Gắn với appsettings.json → "AiPolicy".
    /// </summary>
    public class AiPolicyOptions
    {
        /// <summary>
        /// Nếu = false → AI chỉ trả lời kiến thức nội bộ ECN.
        /// Nếu = true → AI được phép dùng ChatGPT khi không tìm thấy câu trả lời nội bộ.
        /// </summary>
        public bool AllowExternalLlm { get; set; } = false;

        /// <summary>
        /// (Tùy chọn) Cho phép fallback Wikipedia?
        /// Nếu = false → tắt luôn phần web QA.
        /// </summary>
        public bool AllowWikipedia { get; set; } = false;
    }
}

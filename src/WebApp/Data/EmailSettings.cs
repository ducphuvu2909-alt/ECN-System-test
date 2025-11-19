namespace WebApp.Data
{
    /// <summary>
    /// Cấu hình SMTP dùng để gửi email cảnh báo từ ECN Manager.
    /// </summary>
    public class EmailSettings
    {
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "ECN Manager AI";
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WebApp.Data;

namespace WebApp.Services
{
    /// <summary>
    /// Service gửi email dùng SMTP cấu hình trong EmailSettings.
    /// </summary>
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>
        /// Gửi email HTML đơn giản. Nếu chưa cấu hình SMTP thì hàm sẽ return, không làm crash hệ thống.
        /// </summary>
        public async Task SendAsync(string to, string subject, string bodyHtml)
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpHost) ||
                string.IsNullOrWhiteSpace(_settings.FromAddress))
            {
                // Chưa cấu hình SMTP -> bỏ qua, tránh lỗi runtime.
                return;
            }

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            var msg = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };
            msg.To.Add(to);

            await client.SendMailAsync(msg);
        }
    }
}

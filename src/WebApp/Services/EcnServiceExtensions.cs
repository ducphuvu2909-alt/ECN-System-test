using System.Threading.Tasks;

namespace WebApp.Services
{
    /// <summary>
    /// Extension methods cho EcnService dùng riêng cho AI.
    /// Mục đích: tránh lỗi compile khi AiAdvisorService gọi GetOverdueSummaryAsync,
    /// trong khi EcnService chưa triển khai hàm này.
    /// </summary>
    public static class EcnServiceExtensions
    {
        /// <summary>
        /// Stub tạm thời cho AI – hiện tại chỉ trả về chuỗi mô tả.
        /// Nếu sau này anh muốn thống kê Overdue thật, chỉ cần chỉnh lại
        /// nội dung hàm này hoặc chuyển logic sang EcnService.
        /// </summary>
        public static Task<string> GetOverdueSummaryAsync(this EcnService svc, params object[] _)
        {
            // TODO: triển khai thật theo dữ liệu ECN nếu cần.
            return Task.FromResult("Chức năng thống kê Overdue cho AI đang được cập nhật trong hệ thống ECN.");
        }
    }
}

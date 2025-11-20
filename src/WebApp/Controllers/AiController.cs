using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize] // giữ giống logic auth hiện tại của hệ thống
    public class AiController : ControllerBase
    {
        private readonly AiAdvisorService _advisor;

        public AiController(AiAdvisorService advisor)
        {
            _advisor = advisor;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AiAskRequest req)
        {
            var userName = User?.Identity?.Name ?? req.UserName;
            var userDept = req.UserDept;

            var answer = await _advisor.AskAsync(req.Question ?? string.Empty, userName, userDept);
            return Ok(new { answer });
        }
    }

    /// <summary>
    /// DTO cho API /api/ai/ask
    /// </summary>
    public class AiAskRequest
    {
        public string Question { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? UserDept { get; set; }
    }
}

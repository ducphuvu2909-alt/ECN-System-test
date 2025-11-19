using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApp.Data;
using WebApp.Data.Entities;

namespace WebApp.Controllers {
  [ApiController]
  [Route("api/admin/notify")]
  [Authorize]
  public class AdminNotifyController : ControllerBase {
    private readonly EcnDbContext _db;
    public AdminNotifyController(EcnDbContext db){ _db=db; }

    // GET: /api/admin/notify
    [HttpGet]
    public ActionResult<IEnumerable<NotifyDto>> GetAll(){
      var list = _db.AdminNotifySubscriptions
        .OrderBy(n => n.Name)
        .Select(n => new NotifyDto{
          id = n.Id,
          name = n.Name,
          email = n.Email,
          dept = n.Dept,
          evtValid = n.EvtValid,
          evtNew = n.EvtNew,
          evtDeadline = n.EvtDeadline,
          evtJobError = n.EvtJobError,
          channel = n.Channel,
          frequency = n.Frequency,
          note = n.Note
        })
        .ToList();
      return Ok(list);
    }

    // POST: /api/admin/notify
    [HttpPost]
    public ActionResult<NotifyDto> Create([FromBody] NotifyDto dto){
      if(string.IsNullOrWhiteSpace(dto.name) || string.IsNullOrWhiteSpace(dto.email))
        return BadRequest("Name & Email required");

      var entity = new AdminNotificationSubscription{
        Name = dto.name ?? "",
        Email = dto.email ?? "",
        Dept = dto.dept ?? "",
        EvtValid = dto.evtValid,
        EvtNew = dto.evtNew,
        EvtDeadline = dto.evtDeadline,
        EvtJobError = dto.evtJobError,
        Channel = dto.channel ?? "Popup + Email",
        Frequency = dto.frequency ?? "Real-time",
        Note = dto.note ?? ""
      };
      _db.AdminNotifySubscriptions.Add(entity);
      _db.SaveChanges();

      dto.id = entity.Id;
      return Ok(dto);
    }

    public class NotifyDto {
      public int id { get; set; }
      public string? name { get; set; }
      public string? email { get; set; }
      public string? dept { get; set; }
      public bool evtValid { get; set; }
      public bool evtNew { get; set; }
      public bool evtDeadline { get; set; }
      public bool evtJobError { get; set; }
      public string? channel { get; set; }
      public string? frequency { get; set; }
      public string? note { get; set; }
    }
  }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApp.Data;
using WebApp.Data.Entities;

namespace WebApp.Controllers {
  [ApiController]
  [Route("api/admin/jobs")]
  [Authorize]
  public class AdminJobsController : ControllerBase {
    private readonly EcnDbContext _db;
    public AdminJobsController(EcnDbContext db){ _db=db; }

    // GET: /api/admin/jobs
    [HttpGet]
    public ActionResult<IEnumerable<AdminJobDto>> GetAll(){
      var list = _db.AdminJobs
        .OrderBy(j => j.Name)
        .Select(j => new AdminJobDto{
          id = j.Id,
          name = j.Name,
          type = j.Type,
          sourcePath = j.SourcePath,
          schedule = j.Schedule,
          enabled = j.Enabled ? "On" : "Off",
          note = j.Note,
          lastRun = j.LastRunUtc.HasValue ? j.LastRunUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : ""
        })
        .ToList();
      return Ok(list);
    }

    // POST: /api/admin/jobs
    [HttpPost]
    public ActionResult<AdminJobDto> Create([FromBody] AdminJobDto dto){
      if(string.IsNullOrWhiteSpace(dto.name))
        return BadRequest("Name required");

      var entity = new AdminJob{
        Name = dto.name ?? "",
        Type = dto.type ?? "",
        SourcePath = dto.sourcePath ?? "",
        Schedule = dto.schedule ?? "",
        Enabled = (dto.enabled ?? "On") == "On",
        Note = dto.note ?? ""
      };
      _db.AdminJobs.Add(entity);
      _db.SaveChanges();

      dto.id = entity.Id;
      dto.lastRun = "";
      return Ok(dto);
    }

    public class AdminJobDto {
      public int id { get; set; }
      public string? name { get; set; }
      public string? type { get; set; }
      public string? sourcePath { get; set; }
      public string? schedule { get; set; }
      public string? enabled { get; set; }
      public string? note { get; set; }
      public string? lastRun { get; set; }
    }
  }
}

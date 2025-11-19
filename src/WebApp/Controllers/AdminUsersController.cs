using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApp.Data;
using WebApp.Data.Entities;

namespace WebApp.Controllers {
  [ApiController]
  [Route("api/admin/users")]
  [Authorize]
  public class AdminUsersController : ControllerBase {
    private readonly EcnDbContext _db;
    public AdminUsersController(EcnDbContext db){ _db=db; }

    // GET: /api/admin/users
    [HttpGet]
    public ActionResult<IEnumerable<AdminUserDto>> GetAll(){
      var list = _db.AdminUsers
        .OrderBy(u => u.Name)
        .Select(u => new AdminUserDto{
          id = u.Id,
          name = u.Name,
          gid = u.GlobalId,
          email = u.Email,
          dept = u.Dept,
          role = u.Role,
          status = u.Status,
          note = u.Note
        })
        .ToList();
      return Ok(list);
    }

    // POST: /api/admin/users
    [HttpPost]
    public ActionResult<AdminUserDto> Create([FromBody] AdminUserDto dto){
      if(string.IsNullOrWhiteSpace(dto.name) || string.IsNullOrWhiteSpace(dto.gid))
        return BadRequest("Name & GlobalId required");

      var entity = new AdminUserConfig{
        Name = dto.name ?? "",
        GlobalId = dto.gid ?? "",
        Email = dto.email ?? "",
        Dept = dto.dept ?? "",
        Role = dto.role ?? "",
        Status = dto.status ?? "Active",
        Note = dto.note ?? ""
      };
      _db.AdminUsers.Add(entity);
      _db.SaveChanges();

      dto.id = entity.Id;
      return Ok(dto);
    }

    // Simple DTO shaped exactly as Admin UI expects (camelCase)
    public class AdminUserDto {
      public int id { get; set; }
      public string? name { get; set; }
      public string? gid { get; set; }
      public string? email { get; set; }
      public string? dept { get; set; }
      public string? role { get; set; }
      public string? status { get; set; }
      public string? note { get; set; }
    }
  }
}

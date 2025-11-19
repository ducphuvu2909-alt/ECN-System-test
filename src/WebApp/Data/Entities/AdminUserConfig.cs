namespace WebApp.Data.Entities {
  // Admin config for users (separate from login User entity)
  public class AdminUserConfig {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string GlobalId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Dept { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string Note { get; set; } = "";
  }
}

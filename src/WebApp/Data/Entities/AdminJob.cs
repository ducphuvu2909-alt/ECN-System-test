namespace WebApp.Data.Entities {
  // Admin-scheduled job for SAP / ECN sync etc.
  public class AdminJob {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string Schedule { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string Note { get; set; } = "";
    // Last time this job was executed (UTC)
    public DateTime? LastRunUtc { get; set; }
  }
}

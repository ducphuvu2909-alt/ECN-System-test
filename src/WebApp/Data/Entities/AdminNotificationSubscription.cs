namespace WebApp.Data.Entities {
  // Who should receive notifications when ECN / Jobs change
  public class AdminNotificationSubscription {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Dept { get; set; } = "";
    public bool EvtValid { get; set; } = true;
    public bool EvtNew { get; set; } = true;
    public bool EvtDeadline { get; set; } = false;
    public bool EvtJobError { get; set; } = false;
    public string Channel { get; set; } = "Popup + Email";
    public string Frequency { get; set; } = "Real-time";
    public string Note { get; set; } = "";
  }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApp.Data;
using WebApp.Data.Entities;

namespace WebApp.Services {
  /// <summary>
  /// Simple background scheduler: periodically checks AdminJobs in DB
  /// and triggers the appropriate sync logic (SAP, etc.).
  /// This is a skeleton: the actual SAP ingest logic should be implemented
  /// in SapIngestService.
  /// </summary>
  public class EcnJobRunnerHostedService : BackgroundService {
    private readonly IServiceProvider _sp;
    private readonly ILogger<EcnJobRunnerHostedService> _logger;

    public EcnJobRunnerHostedService(IServiceProvider sp, ILogger<EcnJobRunnerHostedService> logger){
      _sp = sp;
      _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken){
      _logger.LogInformation("ECN Job Runner started");
      while(!stoppingToken.IsCancellationRequested){
        try{
          using var scope = _sp.CreateScope();
          var db = scope.ServiceProvider.GetRequiredService<EcnDbContext>();
          var sap = scope.ServiceProvider.GetRequiredService<SapIngestService>();

          var nowUtc = DateTime.UtcNow;
          var jobs = await db.AdminJobs.Where(j => j.Enabled).ToListAsync(stoppingToken);

          foreach(var job in jobs){
            if(IsDue(job, nowUtc)){
              _logger.LogInformation("Running job {Name} ({Type})", job.Name, job.Type);
              // TODO: tùy theo job.Type mà gọi SapIngestService / service khác
              // Ví dụ:
              // if(job.Type == "SAP Valid BOM Sync"){
              //   await sap.SyncValidBomAsync(job.SourcePath, stoppingToken);
              // }

              job.LastRunUtc = nowUtc;
              db.AdminJobs.Update(job);
              await db.SaveChangesAsync(stoppingToken);
            }
          }
        }catch(Exception ex){
          _logger.LogError(ex, "Error while running ECN jobs");
        }

        // Simple polling every 1 minute
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
      }
      _logger.LogInformation("ECN Job Runner stopping");
    }

    private bool IsDue(AdminJob job, DateTime nowUtc){
      if(job.LastRunUtc == null) return true;
      var interval = GetInterval(job);
      return (nowUtc - job.LastRunUtc.Value) >= interval;
    }

    /// <summary>
    /// Đọc chuỗi Schedule dạng text ("Mỗi 15 phút", "Hàng ngày 06:00"...)
    /// và tạm ánh xạ thành chu kỳ đơn giản. Có thể nâng cấp sau dùng cron.
    /// </summary>
    private TimeSpan GetInterval(AdminJob job){
      var s = (job.Schedule ?? string.Empty).ToLowerInvariant();
      if(s.Contains("15")) return TimeSpan.FromMinutes(15);
      if(s.Contains("30")) return TimeSpan.FromMinutes(30);
      if(s.Contains("1 giờ") || s.Contains("1h") || s.Contains("1 gio")) return TimeSpan.FromHours(1);
      if(s.Contains("hàng ngày") || s.Contains("hang ngay")) return TimeSpan.FromHours(24);
      // default
      return TimeSpan.FromMinutes(30);
    }
  }
}

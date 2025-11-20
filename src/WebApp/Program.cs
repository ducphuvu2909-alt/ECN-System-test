using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using WebApp.Services;
using WebApp.Data;
using Microsoft.AspNetCore.Authorization;

// Đặt alias để tránh trùng tên OpenAiOptions giữa WebApp.AI và WebApp.Data
using AiOptions = WebApp.AI.OpenAiOptions;
using AiClient = WebApp.AI.OpenAiClient;

var builder = WebApplication.CreateBuilder(args);

// Background scheduler for Admin Jobs
builder.Services.AddHostedService<EcnJobRunnerHostedService>();

// =========================
// APP CONFIG
// =========================
var cfg = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// =========================
 // JWT AUTH (GIỮ NGUYÊN)
// =========================
var jwtKey = cfg["Jwt:Key"] ?? "CHANGE_ME_TO_A_LONG_RANDOM_SECRET";
var issuer = cfg["Jwt:Issuer"] ?? "ECNManager";
var audience = cfg["Jwt:Audience"] ?? "ECNClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o => {
    o.TokenValidationParameters = new TokenValidationParameters {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer = issuer,
      ValidAudience = audience,
      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
  });

builder.Services.AddAuthorization();

// =========================
// DATABASE (GIỮ NGUYÊN)
// =========================
var connStr = cfg.GetConnectionString("EcnDb") ?? "Data Source=ecn.db;Version=3;";
builder.Services.AddDbContext<EcnDbContext>(o => o.UseSqlite(connStr));

// =========================
// ORIGINAL SERVICES (GIỮ NGUYÊN)
// =========================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EcnService>();
builder.Services.AddScoped<DeptService>();
builder.Services.AddScoped<NotifyService>();
builder.Services.AddScoped<SapIngestService>();

// =========================
// AI MODULE
// =========================

// 1) Chính sách AI nội bộ / external
builder.Services.Configure<AiPolicyOptions>(cfg.GetSection("Ai"));

// 2) Setting ChatGPT (nếu bật external)
//    ApiKey sẽ lấy từ env var: Ai__OpenAI__ApiKey
builder.Services.Configure<AiOptions>(cfg.GetSection("Ai:OpenAI"));

// 3) HttpClient → ChatGPT
builder.Services.AddHttpClient<AiClient>();

// 4) AI Advisor Service
builder.Services.AddScoped<AiAdvisorService>();

// =========================
// BUILD APP
// =========================
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// =========================
// INIT DB SEED (GIỮ NGUYÊN)
// =========================
using (var scope = app.Services.CreateScope()){
  var ctx = scope.ServiceProvider.GetRequiredService<EcnDbContext>();
  ctx.Database.EnsureCreated();
  Seed.Run(ctx);
}

app.Run();

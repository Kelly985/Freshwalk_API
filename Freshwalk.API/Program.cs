using System.Text.Json;
using Freshwalk.API.Configuration;
using Freshwalk.API.Logging;
using Freshwalk.Infrastructure;
using Freshwalk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Events;

// QuestPDF community licence (free for small businesses)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Render and other hosts set PORT; bind Kestrel explicitly (Docker / PaaS).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var auditLogSection = builder.Configuration.GetSection(AuditLoggingOptions.SectionName).Get<AuditLoggingOptions>();
var logRoot = string.IsNullOrWhiteSpace(auditLogSection?.RootPath)
    ? Path.Combine(Directory.GetCurrentDirectory(), "Logs")
    : Path.GetFullPath(auditLogSection!.RootPath);
var appLogDir = Path.Combine(logRoot, "application");
Directory.CreateDirectory(appLogDir);

builder.Host.UseSerilog((_, _, cfg) =>
{
    cfg.MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(appLogDir, "freshwalk-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31);
});

builder.Services.Configure<AuditLoggingOptions>(builder.Configuration.GetSection(AuditLoggingOptions.SectionName));
builder.Services.AddScoped<AuditRequestLoggingMiddleware>();

// Allow up to 200 MB per file (two files per gallery upload = 200 MB each).
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    o.MultipartBodyLengthLimit = 210_000_000);
builder.WebHost.ConfigureKestrel(o =>
    o.Limits.MaxRequestBodySize = 210_000_000);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

var corsRaw = builder.Configuration["Cors:AllowedOrigins"];
string[] corsOrigins;
if (string.IsNullOrWhiteSpace(corsRaw))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "Cors:AllowedOrigins must be set in production (comma-separated front-end URLs, e.g. https://yourdomain.com,https://agent.yourdomain.com).");
    corsOrigins =
    [
        "http://localhost:5173",
        "http://localhost:5174",
        "http://localhost:5175"
    ];
}
else
{
    corsOrigins = corsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditRequestLoggingMiddleware>();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await CatalogSeeder.EnsureShoeCatalogAsync(app.Services);
await CatalogSeeder.EnsureSamplePromotionsAsync(app.Services);

var authSeed = builder.Configuration.GetSection("AuthSeed").Get<AuthSeedSettings>() ?? new AuthSeedSettings();
await AuthSeeder.SeedDefaultUsersAsync(app.Services, authSeed);

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

using Serilog;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using AntiCheatService.Core.Detection;
using AntiCheatService.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/anticheat-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=postgres;Port=5432;Database=anticheat_db;Username=anticheat;Password=SecurePassword123!";

builder.Services.AddDbContext<AntiCheatDbContext>(options =>
    options.UseNpgsql(connectionString));

var redisConfig = ConfigurationOptions.Parse(builder.Configuration["Redis:Configuration"] ?? "redis:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConfig));

builder.Services.AddSingleton<ITelemetryAnalyzer, TelemetryAnalyzer>();
builder.Services.AddScoped<IDetectionEngine, DetectionEngine>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRouting();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AntiCheatDbContext>();
    db.Database.Migrate();
}

app.Run();

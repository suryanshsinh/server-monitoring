using Microsoft.EntityFrameworkCore;
using ServerMonitor.Api.Data;
using ServerMonitor.Api.Services;
using ServerMonitor.Api.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuration
builder.Services.Configure<MetricCollectionOptions>(
    builder.Configuration.GetSection("MetricCollection"));

// Services
builder.Services.AddSingleton<ISshKeyService, SshKeyService>();
builder.Services.AddScoped<ISshMetricService, SshMetricService>();
builder.Services.AddScoped<IServerService, ServerService>();

// Background service for metric collection
builder.Services.AddHostedService<MetricCollectorJob>();

// Controllers
builder.Services.AddControllers();

// CORS for Angular dashboard
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://dashboard:80")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowDashboard");
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServerMonitor.Api.Data;
using ServerMonitor.Api.Services;

namespace ServerMonitor.Api.BackgroundJobs;

public class MetricCollectorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricCollectorJob> _logger;
    private readonly MetricCollectionOptions _options;

    public MetricCollectorJob(
        IServiceProvider serviceProvider,
        ILogger<MetricCollectorJob> logger,
        IOptions<MetricCollectionOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metric collector started. Interval: {Interval}s", _options.IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAllMetricsAsync(stoppingToken);
                await CleanupOldDataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during metric collection cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
        }
    }

    private async Task CollectAllMetricsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sshService = scope.ServiceProvider.GetRequiredService<ISshMetricService>();

        var activeServers = await db.Servers
            .Where(s => s.IsActive)
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Collecting metrics from {Count} active servers", activeServers.Count);

        var tasks = activeServers.Select(async server =>
        {
            var result = await sshService.CollectMetricsAsync(server);
            if (result.HasValue)
            {
                return (server.Id, result.Value.metrics, result.Value.services);
            }
            return (server.Id, (Models.MetricSnapshot?)null, (List<Models.ServiceStatus>?)null);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (serverId, metrics, services) in results)
        {
            if (metrics != null)
            {
                db.MetricSnapshots.Add(metrics);
                _logger.LogDebug("Collected metrics for server {ServerId}: CPU={Cpu}%, Memory={Mem}MB",
                    serverId, metrics.CpuPercent, metrics.MemoryUsedMb);
            }

            if (services != null && services.Count > 0)
            {
                db.ServiceStatuses.AddRange(services);
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }

    private async Task CleanupOldDataAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        var deletedMetrics = await db.MetricSnapshots
            .Where(m => m.Timestamp < cutoffDate)
            .ExecuteDeleteAsync(stoppingToken);

        var deletedServices = await db.ServiceStatuses
            .Where(s => s.Timestamp < cutoffDate)
            .ExecuteDeleteAsync(stoppingToken);

        if (deletedMetrics > 0 || deletedServices > 0)
        {
            _logger.LogInformation("Cleaned up {Metrics} old metrics and {Services} old service statuses",
                deletedMetrics, deletedServices);
        }
    }
}

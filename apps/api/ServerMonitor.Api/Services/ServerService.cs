using Microsoft.EntityFrameworkCore;
using ServerMonitor.Api.Data;
using ServerMonitor.Api.Models;

namespace ServerMonitor.Api.Services;

public class ServerService : IServerService
{
    private readonly AppDbContext _db;
    private readonly ISshKeyService _sshKeyService;

    public ServerService(AppDbContext db, ISshKeyService sshKeyService)
    {
        _db = db;
        _sshKeyService = sshKeyService;
    }

    public async Task<List<ServerDto>> GetAllServersAsync()
    {
        var servers = await _db.Servers.ToListAsync();
        var result = new List<ServerDto>();

        foreach (var server in servers)
        {
            var health = await GetCurrentHealthAsync(server.Id);
            var publicKey = _sshKeyService.GetPublicKey(server.Id);
            result.Add(MapToDto(server, health, publicKey));
        }

        return result;
    }

    public async Task<ServerDto?> GetServerByIdAsync(int id)
    {
        var server = await _db.Servers.FindAsync(id);
        if (server == null) return null;

        var health = await GetCurrentHealthAsync(id);
        var publicKey = _sshKeyService.GetPublicKey(id);
        return MapToDto(server, health, publicKey);
    }

    public async Task<ServerDto> CreateServerAsync(CreateServerRequest request)
    {
        var server = new Server
        {
            Name = request.Name,
            Host = request.Host,
            Port = request.Port,
            Username = request.Username,
            IsActive = false,
            MonitoredServices = request.MonitoredServices ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Servers.Add(server);
        await _db.SaveChangesAsync();

        var (privateKeyPath, publicKey) = _sshKeyService.GenerateKeyPair(server.Id, server.Name);
        
        server.SshKeyPath = privateKeyPath;
        await _db.SaveChangesAsync();

        return MapToDto(server, null, publicKey);
    }

    public async Task<ServerDto?> UpdateServerAsync(int id, UpdateServerRequest request)
    {
        var server = await _db.Servers.FindAsync(id);
        if (server == null) return null;

        if (request.Name != null) server.Name = request.Name;
        if (request.Host != null) server.Host = request.Host;
        if (request.Port.HasValue) server.Port = request.Port.Value;
        if (request.Username != null) server.Username = request.Username;
        if (request.IsActive.HasValue) server.IsActive = request.IsActive.Value;
        if (request.MonitoredServices != null) server.MonitoredServices = request.MonitoredServices;

        await _db.SaveChangesAsync();

        var health = await GetCurrentHealthAsync(id);
        var publicKey = _sshKeyService.GetPublicKey(id);
        return MapToDto(server, health, publicKey);
    }

    public async Task<bool> DeleteServerAsync(int id)
    {
        var server = await _db.Servers.FindAsync(id);
        if (server == null) return false;

        _sshKeyService.DeleteKeyPair(id);
        
        _db.Servers.Remove(server);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<MetricSnapshotDto>> GetServerMetricsAsync(int serverId, DateTime? from, DateTime? to, int? limit)
    {
        var query = _db.MetricSnapshots
            .Where(m => m.ServerId == serverId)
            .OrderByDescending(m => m.Timestamp)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(m => m.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(m => m.Timestamp <= to.Value);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        var metrics = await query.ToListAsync();

        return metrics.Select(m => new MetricSnapshotDto(
            m.Id,
            m.ServerId,
            m.Timestamp,
            m.CpuPercent,
            m.MemoryUsedMb,
            m.MemoryTotalMb,
            m.MemoryTotalMb > 0 ? Math.Round((decimal)m.MemoryUsedMb / m.MemoryTotalMb * 100, 2) : 0
        )).ToList();
    }

    public async Task<List<ServiceStatusDto>> GetServerServicesAsync(int serverId)
    {
        var latestTimestamp = await _db.ServiceStatuses
            .Where(s => s.ServerId == serverId)
            .MaxAsync(s => (DateTime?)s.Timestamp);

        if (!latestTimestamp.HasValue)
            return new List<ServiceStatusDto>();

        var services = await _db.ServiceStatuses
            .Where(s => s.ServerId == serverId && s.Timestamp == latestTimestamp.Value)
            .ToListAsync();

        return services.Select(s => new ServiceStatusDto(
            s.Id,
            s.ServerId,
            s.Timestamp,
            s.ServiceName,
            s.IsRunning,
            s.Status
        )).ToList();
    }

    private async Task<ServerHealthDto?> GetCurrentHealthAsync(int serverId)
    {
        var latestMetric = await _db.MetricSnapshots
            .Where(m => m.ServerId == serverId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();

        if (latestMetric == null) return null;

        var memoryPercent = latestMetric.MemoryTotalMb > 0
            ? Math.Round((decimal)latestMetric.MemoryUsedMb / latestMetric.MemoryTotalMb * 100, 2)
            : 0;

        var status = DetermineHealthStatus(latestMetric.CpuPercent, memoryPercent, latestMetric.Timestamp);

        return new ServerHealthDto(
            latestMetric.CpuPercent,
            latestMetric.MemoryUsedMb,
            latestMetric.MemoryTotalMb,
            memoryPercent,
            latestMetric.Timestamp,
            status
        );
    }

    private static string DetermineHealthStatus(decimal cpuPercent, decimal memoryPercent, DateTime lastUpdated)
    {
        if (DateTime.UtcNow - lastUpdated > TimeSpan.FromMinutes(5))
            return "offline";

        if (cpuPercent > 90 || memoryPercent > 90)
            return "critical";

        if (cpuPercent > 70 || memoryPercent > 70)
            return "warning";

        return "healthy";
    }

    private static ServerDto MapToDto(Server server, ServerHealthDto? health, string? publicKey) => new(
        server.Id,
        server.Name,
        server.Host,
        server.Port,
        server.Username,
        server.IsActive,
        server.MonitoredServices,
        server.CreatedAt,
        health,
        publicKey
    );
}

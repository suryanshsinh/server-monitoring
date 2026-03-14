using ServerMonitor.Api.Models;

namespace ServerMonitor.Api.Services;

public interface IServerService
{
    Task<List<ServerDto>> GetAllServersAsync();
    Task<ServerDto?> GetServerByIdAsync(int id);
    Task<ServerDto> CreateServerAsync(CreateServerRequest request);
    Task<ServerDto?> UpdateServerAsync(int id, UpdateServerRequest request);
    Task<bool> DeleteServerAsync(int id);
    Task<List<MetricSnapshotDto>> GetServerMetricsAsync(int serverId, DateTime? from, DateTime? to, int? limit);
    Task<List<ServiceStatusDto>> GetServerServicesAsync(int serverId);
}

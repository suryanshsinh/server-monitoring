using ServerMonitor.Api.Models;

namespace ServerMonitor.Api.Services;

public interface ISshMetricService
{
    Task<TestConnectionResult> TestConnectionAsync(Server server);
    Task<(MetricSnapshot metrics, List<ServiceStatus> services)?> CollectMetricsAsync(Server server);
    Task<SetupSshKeyResult> SetupSshKeyAsync(Server server, string password, string publicKey);
}

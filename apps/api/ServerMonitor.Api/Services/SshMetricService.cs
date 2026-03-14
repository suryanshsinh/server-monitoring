using Renci.SshNet;
using ServerMonitor.Api.Models;

namespace ServerMonitor.Api.Services;

public class SshMetricService : ISshMetricService
{
    private readonly ILogger<SshMetricService> _logger;
    private readonly ISshKeyService _sshKeyService;

    public SshMetricService(ILogger<SshMetricService> logger, ISshKeyService sshKeyService)
    {
        _logger = logger;
        _sshKeyService = sshKeyService;
    }

    public async Task<TestConnectionResult> TestConnectionAsync(Server server)
    {
        try
        {
            using var client = CreateSshClient(server);
            await Task.Run(() => client.Connect());

            var hostnameResult = ExecuteCommand(client, "hostname");
            var osResult = ExecuteCommand(client, "cat /etc/os-release | grep PRETTY_NAME | cut -d'\"' -f2");
            var cpuCoresResult = ExecuteCommand(client, "nproc");
            var memoryResult = ExecuteCommand(client, "free -m | grep Mem | awk '{print $2}'");

            client.Disconnect();

            return new TestConnectionResult(
                true,
                "Connection successful",
                new ServerSystemInfo(
                    hostnameResult.Trim(),
                    osResult.Trim(),
                    int.TryParse(cpuCoresResult.Trim(), out var cores) ? cores : 0,
                    int.TryParse(memoryResult.Trim(), out var mem) ? mem : 0
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to server {Host}", server.Host);
            return new TestConnectionResult(false, $"Connection failed: {ex.Message}", null);
        }
    }

    public async Task<(MetricSnapshot metrics, List<ServiceStatus> services)?> CollectMetricsAsync(Server server)
    {
        try
        {
            using var client = CreateSshClient(server);
            await Task.Run(() => client.Connect());

            var cpuResult = ExecuteCommand(client, "top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1");
            var memoryResult = ExecuteCommand(client, "free -m | grep Mem | awk '{print $2,$3}'");

            var cpuPercent = decimal.TryParse(cpuResult.Trim().Replace(',', '.'), 
                System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, 
                out var cpu) ? cpu : 0;

            var memParts = memoryResult.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var memTotal = memParts.Length > 0 && int.TryParse(memParts[0], out var mt) ? mt : 0;
            var memUsed = memParts.Length > 1 && int.TryParse(memParts[1], out var mu) ? mu : 0;

            var metrics = new MetricSnapshot
            {
                ServerId = server.Id,
                Timestamp = DateTime.UtcNow,
                CpuPercent = cpuPercent,
                MemoryTotalMb = memTotal,
                MemoryUsedMb = memUsed
            };

            var services = new List<ServiceStatus>();
            foreach (var serviceName in server.MonitoredServices)
            {
                var statusResult = ExecuteCommand(client, $"systemctl is-active {serviceName} 2>/dev/null || echo 'unknown'");
                var status = statusResult.Trim().ToLower();
                var isRunning = status == "active";

                services.Add(new ServiceStatus
                {
                    ServerId = server.Id,
                    Timestamp = DateTime.UtcNow,
                    ServiceName = serviceName,
                    IsRunning = isRunning,
                    Status = status
                });
            }

            client.Disconnect();
            return (metrics, services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect metrics from server {Name}", server.Name);
            return null;
        }
    }

    public async Task<SetupSshKeyResult> SetupSshKeyAsync(Server server, string password, string publicKey)
    {
        try
        {
            using var client = new SshClient(server.Host, server.Port, server.Username, password);
            await Task.Run(() => client.Connect());

            var setupCommand = $@"
mkdir -p ~/.ssh && 
chmod 700 ~/.ssh && 
echo '{publicKey}' >> ~/.ssh/authorized_keys && 
chmod 600 ~/.ssh/authorized_keys && 
echo 'SSH key added successfully'";

            var result = ExecuteCommand(client, setupCommand);
            client.Disconnect();

            if (result.Contains("SSH key added successfully"))
            {
                _logger.LogInformation("SSH key setup completed for server {Name}", server.Name);
                return new SetupSshKeyResult(true, "SSH key installed successfully. You can now activate monitoring.");
            }

            return new SetupSshKeyResult(false, $"Setup completed but verification failed: {result}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup SSH key for server {Name}", server.Name);
            
            var message = ex.Message.Contains("Authentication failed") 
                ? "Invalid password. Please check your credentials."
                : $"Setup failed: {ex.Message}";
                
            return new SetupSshKeyResult(false, message);
        }
    }

    private SshClient CreateSshClient(Server server)
    {
        var keyPath = server.SshKeyPath ?? _sshKeyService.GetPrivateKeyPath(server.Id);
        
        if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath))
        {
            throw new InvalidOperationException(
                $"SSH key not found for server '{server.Name}'. " +
                $"Expected at: {keyPath}. " +
                "Please ensure the server was created through the dashboard."
            );
        }

        _logger.LogDebug("Connecting to {Host} using key {KeyPath}", server.Host, keyPath);
        var keyFile = new PrivateKeyFile(keyPath);
        return new SshClient(server.Host, server.Port, server.Username, keyFile);
    }

    private static string ExecuteCommand(SshClient client, string command)
    {
        using var cmd = client.CreateCommand(command);
        return cmd.Execute();
    }
}

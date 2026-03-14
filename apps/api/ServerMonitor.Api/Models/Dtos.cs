namespace ServerMonitor.Api.Models;

public record ServerDto(
    int Id,
    string Name,
    string Host,
    int Port,
    string Username,
    bool IsActive,
    List<string> MonitoredServices,
    DateTime CreatedAt,
    ServerHealthDto? CurrentHealth,
    string? PublicKey
);

public record ServerHealthDto(
    decimal CpuPercent,
    int MemoryUsedMb,
    int MemoryTotalMb,
    decimal MemoryPercent,
    DateTime LastUpdated,
    string Status
);

public record CreateServerRequest(
    string Name,
    string Host,
    int Port,
    string Username,
    List<string>? MonitoredServices
);

public record UpdateServerRequest(
    string? Name,
    string? Host,
    int? Port,
    string? Username,
    bool? IsActive,
    List<string>? MonitoredServices
);

public record MetricSnapshotDto(
    long Id,
    int ServerId,
    DateTime Timestamp,
    decimal CpuPercent,
    int MemoryUsedMb,
    int MemoryTotalMb,
    decimal MemoryPercent
);

public record ServiceStatusDto(
    long Id,
    int ServerId,
    DateTime Timestamp,
    string ServiceName,
    bool IsRunning,
    string Status
);

public record MetricsQueryParams(
    DateTime? From,
    DateTime? To,
    int? Limit
);

public record TestConnectionResult(
    bool Success,
    string Message,
    ServerSystemInfo? SystemInfo
);

public record ServerSystemInfo(
    string Hostname,
    string OsVersion,
    int CpuCores,
    int TotalMemoryMb
);

public record SetupSshKeyRequest(
    string Password
);

public record SetupSshKeyResult(
    bool Success,
    string Message
);

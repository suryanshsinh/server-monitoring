namespace ServerMonitor.Api.BackgroundJobs;

public class MetricCollectionOptions
{
    public int IntervalSeconds { get; set; } = 30;
    public int RetentionDays { get; set; } = 30;
    public string SshKeyBasePath { get; set; } = "/app/ssh-keys";
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerMonitor.Api.Models;

[Table("metric_snapshots")]
public class MetricSnapshot
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("server_id")]
    public int ServerId { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("cpu_percent", TypeName = "decimal(5,2)")]
    public decimal CpuPercent { get; set; }

    [Column("memory_used_mb")]
    public int MemoryUsedMb { get; set; }

    [Column("memory_total_mb")]
    public int MemoryTotalMb { get; set; }

    [ForeignKey(nameof(ServerId))]
    public virtual Server Server { get; set; } = null!;
}

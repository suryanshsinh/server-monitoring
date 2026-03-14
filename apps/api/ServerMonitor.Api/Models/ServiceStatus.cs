using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerMonitor.Api.Models;

[Table("service_statuses")]
public class ServiceStatus
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("server_id")]
    public int ServerId { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    [Column("service_name")]
    public string ServiceName { get; set; } = string.Empty;

    [Column("is_running")]
    public bool IsRunning { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [ForeignKey(nameof(ServerId))]
    public virtual Server Server { get; set; } = null!;
}

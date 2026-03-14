using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerMonitor.Api.Models;

[Table("servers")]
public class Server
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("host")]
    public string Host { get; set; } = string.Empty;

    [Column("port")]
    public int Port { get; set; } = 22;

    [Required]
    [MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("ssh_key_path")]
    public string? SshKeyPath { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("monitored_services", TypeName = "text[]")]
    public List<string> MonitoredServices { get; set; } = new();

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<MetricSnapshot> MetricSnapshots { get; set; } = new List<MetricSnapshot>();
    public virtual ICollection<ServiceStatus> ServiceStatuses { get; set; } = new List<ServiceStatus>();
}

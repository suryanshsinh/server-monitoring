using Microsoft.EntityFrameworkCore;
using ServerMonitor.Api.Models;

namespace ServerMonitor.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Server> Servers => Set<Server>();
    public DbSet<MetricSnapshot> MetricSnapshots => Set<MetricSnapshot>();
    public DbSet<ServiceStatus> ServiceStatuses => Set<ServiceStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasIndex(e => e.Host).IsUnique();
        });

        modelBuilder.Entity<MetricSnapshot>(entity =>
        {
            entity.HasIndex(e => new { e.ServerId, e.Timestamp })
                  .IsDescending(false, true)
                  .HasDatabaseName("idx_metrics_server_time");

            entity.HasOne(m => m.Server)
                  .WithMany(s => s.MetricSnapshots)
                  .HasForeignKey(m => m.ServerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceStatus>(entity =>
        {
            entity.HasIndex(e => new { e.ServerId, e.Timestamp })
                  .IsDescending(false, true)
                  .HasDatabaseName("idx_services_server_time");

            entity.HasOne(s => s.Server)
                  .WithMany(srv => srv.ServiceStatuses)
                  .HasForeignKey(s => s.ServerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

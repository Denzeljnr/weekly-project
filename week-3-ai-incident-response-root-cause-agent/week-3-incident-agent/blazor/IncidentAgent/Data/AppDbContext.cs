using Microsoft.EntityFrameworkCore;
using IncidentAgent.Models;

namespace IncidentAgent.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // maps to the demo app's existing tables
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<DbEvent> DbEvents => Set<DbEvent>();
    public DbSet<Alert> Alerts => Set<Alert>();

    // this agent's own table
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogEntry>(e =>
        {
            e.ToTable("logs", t => t.ExcludeFromMigrations());
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Level).HasColumnName("level");
            e.Property(p => p.Endpoint).HasColumnName("endpoint");
            e.Property(p => p.Message).HasColumnName("message");
            e.Property(p => p.ResponseTimeMs).HasColumnName("response_time_ms");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Deployment>(e =>
        {
            e.ToTable("deployments", t => t.ExcludeFromMigrations());
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.CommitSha).HasColumnName("commit_sha");
            e.Property(p => p.Description).HasColumnName("description");
            e.Property(p => p.DeployedAt).HasColumnName("deployed_at");
        });

        modelBuilder.Entity<DbEvent>(e =>
        {
            e.ToTable("db_events", t => t.ExcludeFromMigrations());
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.EventType).HasColumnName("event_type");
            e.Property(p => p.Detail).HasColumnName("detail");
            e.Property(p => p.DurationMs).HasColumnName("duration_ms");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.ToTable("alerts", t => t.ExcludeFromMigrations());
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Metric).HasColumnName("metric");
            e.Property(p => p.ThresholdValue).HasColumnName("threshold_value");
            e.Property(p => p.ActualValue).HasColumnName("actual_value");
            e.Property(p => p.Message).HasColumnName("message");
            e.Property(p => p.FiredAt).HasColumnName("fired_at");
        });

        // No ExcludeFromMigrations here — Blazor DOES own and create this table.
        modelBuilder.Entity<Incident>(e =>
        {
            e.ToTable("incidents");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.AlertId).HasColumnName("alert_id");
            e.Property(p => p.Summary).HasColumnName("summary");
            e.Property(p => p.LikelyCause).HasColumnName("likely_cause");
            e.Property(p => p.Evidence).HasColumnName("evidence");
            e.Property(p => p.AffectedService).HasColumnName("affected_service");
            e.Property(p => p.ConfidenceScore).HasColumnName("confidence_score");
            e.Property(p => p.RecommendedAction).HasColumnName("recommended_action");
            e.Property(p => p.RelatedEventsCount).HasColumnName("related_events_count");
            e.Property(p => p.Source).HasColumnName("source");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
        });
    }
}
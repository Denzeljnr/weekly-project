using Microsoft.EntityFrameworkCore;
using WriteAngle.Models;

namespace WriteAngle.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("reports");
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.Topic).HasColumnName("topic");
            entity.Property(r => r.FinalText).HasColumnName("final_text");
            entity.Property(r => r.RevisionCount).HasColumnName("revision_count");
            entity.Property(r => r.SourcesJson).HasColumnName("sources_json");
            entity.Property(r => r.CreatedAt).HasColumnName("created_at")
                .HasDefaultValueSql("now()");
        });
    }
}
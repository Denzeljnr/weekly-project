using Microsoft.EntityFrameworkCore;
using SemanticSearch.Models;

namespace SemanticSearch.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Chunk> Chunks => Set<Chunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<Chunk>()
            .Property(c => c.Embedding)
            .HasColumnType("vector(768)"); // confirm this matches your embedding model's actual output size — see Step 9
    }
}
using Microsoft.EntityFrameworkCore;
using JobTracker.Models;

namespace JobTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<ProcessedEmail> ProcessedEmails => Set<ProcessedEmail>();
}
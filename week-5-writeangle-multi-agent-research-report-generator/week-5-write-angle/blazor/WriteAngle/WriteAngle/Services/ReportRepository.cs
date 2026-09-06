using Microsoft.EntityFrameworkCore;
using WriteAngle.Data;
using WriteAngle.Interfaces;
using WriteAngle.Models;

namespace WriteAngle.Services;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;

    public ReportRepository(AppDbContext db) => _db = db;

    public async Task SaveAsync(Report report)
    {
        _db.Reports.Add(report);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Report>> GetAllAsync() =>
        await _db.Reports.OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<Report?> GetByIdAsync(int id) =>
        await _db.Reports.FirstOrDefaultAsync(r => r.Id == id);
}
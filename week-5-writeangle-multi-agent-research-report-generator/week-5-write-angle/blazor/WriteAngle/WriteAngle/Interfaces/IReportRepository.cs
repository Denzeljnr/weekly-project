using WriteAngle.Models;

namespace WriteAngle.Interfaces;

public interface IReportRepository
{
    Task SaveAsync(Report report);
    Task<List<Report>> GetAllAsync();
    Task<Report?> GetByIdAsync(int id);
}
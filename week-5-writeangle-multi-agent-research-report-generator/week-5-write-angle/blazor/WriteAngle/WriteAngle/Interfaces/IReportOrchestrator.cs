using WriteAngle.Models;

namespace WriteAngle.Interfaces;

public interface IReportOrchestrator
{
    Task<Report> GenerateReportAsync(string topic);
}
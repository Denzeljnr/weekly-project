using WriteAngle.Models;

namespace WriteAngle.Interfaces;

public interface IPdfExportService
{
    byte[] Export(Report report);
}
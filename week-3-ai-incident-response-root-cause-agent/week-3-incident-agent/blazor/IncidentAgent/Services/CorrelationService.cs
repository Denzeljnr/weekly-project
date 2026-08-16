using Microsoft.EntityFrameworkCore;
using IncidentAgent.Data;
using IncidentAgent.Models;

namespace IncidentAgent.Services;

public record CorrelatedEvidence(
    Alert Alert,
    List<LogEntry> RecentLogs,
    List<Deployment> RecentDeployments,
    List<DbEvent> RecentDbEvents,
    int ConfidenceScore,
    int RelatedEventsCount
);

public interface ICorrelationService
{
    Task<CorrelatedEvidence> GatherEvidenceAsync(Alert alert);
}

public class CorrelationService : ICorrelationService
{
    private readonly AppDbContext _db;

    public CorrelationService(AppDbContext db) => _db = db;

    public async Task<CorrelatedEvidence> GatherEvidenceAsync(Alert alert)
    {
        var firedAtUtc = DateTime.SpecifyKind(alert.FiredAt, DateTimeKind.Utc);
        var windowStart = firedAtUtc.AddMinutes(-10);
        var windowEnd = firedAtUtc.AddMinutes(1);

        var logs = await _db.Logs
            .Where(l => l.CreatedAt >= windowStart && l.CreatedAt <= windowEnd && l.Level == "error")
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        var deployments = await _db.Deployments
            .Where(d => d.DeployedAt >= windowStart && d.DeployedAt <= windowEnd)
            .OrderByDescending(d => d.DeployedAt)
            .ToListAsync();

        var dbEvents = await _db.DbEvents
            .Where(e => e.CreatedAt >= windowStart && e.CreatedAt <= windowEnd)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        // confidence: how many of the 3 possible signal types actually showed up
        int signalTypesPresent = 0;
        if (logs.Any()) signalTypesPresent++;
        if (deployments.Any()) signalTypesPresent++;
        if (dbEvents.Any()) signalTypesPresent++;

        int confidence = (signalTypesPresent * 100) / 3; // simple proportional score
        int relatedEvents = logs.Count + deployments.Count + dbEvents.Count;

        return new CorrelatedEvidence(alert, logs, deployments, dbEvents, confidence, relatedEvents);
    }
}
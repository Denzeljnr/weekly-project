using Microsoft.EntityFrameworkCore;
using IncidentAgent.Data;
using IncidentAgent.Models;

namespace IncidentAgent.Services;

public class IncidentWatcherService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<IncidentWatcherService> _logger;
    private int? _lastSeenAlertId = null; // null until we've loaded it from the DB once

    public IncidentWatcherService(IServiceProvider services, ILogger<IncidentWatcherService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentDelay = TimeSpan.FromSeconds(15);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForNewAlerts();
                currentDelay = TimeSpan.FromSeconds(15);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Incident check failed");
                currentDelay = TimeSpan.FromMinutes(2);
            }
            await Task.Delay(currentDelay, stoppingToken);
        }
    }

    private async Task CheckForNewAlerts()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var correlation = scope.ServiceProvider.GetRequiredService<ICorrelationService>();
        var diagnosis = scope.ServiceProvider.GetRequiredService<IDiagnosisService>();

        // On the very first check after a (re)start, anchor to whatever we've
        // already diagnosed before, instead of assuming nothing has ever run.
        if (_lastSeenAlertId == null)
        {
            var maxProcessed = await db.Incidents
                .Where(i => i.Source == "blazor")
                .Select(i => (int?)i.AlertId)
                .MaxAsync();

            _lastSeenAlertId = maxProcessed ?? 0;
            _logger.LogInformation("Initialized last seen alert ID from database: {LastSeenAlertId}", _lastSeenAlertId);
        }

        var newAlerts = await db.Alerts
            .Where(a => a.Id > _lastSeenAlertId)
            .OrderBy(a => a.Id)
            .ToListAsync();

        foreach (var alert in newAlerts)
        {
            _lastSeenAlertId = alert.Id;

            var evidence = await correlation.GatherEvidenceAsync(alert);
            var result = await diagnosis.DiagnoseAsync(evidence);

            db.Incidents.Add(new Incident
            {
                AlertId = alert.Id,
                Summary = result.Summary,
                LikelyCause = result.LikelyCause,
                Evidence = result.Evidence,
                AffectedService = result.AffectedService,
                ConfidenceScore = evidence.ConfidenceScore,
                RecommendedAction = result.RecommendedAction,
                RelatedEventsCount = evidence.RelatedEventsCount,
                Source = "blazor"
            });
            await db.SaveChangesAsync();

            _logger.LogInformation("Diagnosed incident for alert {AlertId}: {Summary}", alert.Id, result.Summary);
        }
    }
}
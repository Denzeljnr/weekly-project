using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;

namespace JobTracker.Services;

public class GmailPollingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GmailPollingService> _logger;

    public GmailPollingService(IServiceProvider services, ILogger<GmailPollingService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PollOnce(); }
            catch (Exception ex) { _logger.LogError(ex, "Gmail poll failed"); }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task PollOnce()
    {
        using var scope = _services.CreateScope();
        var gmail = scope.ServiceProvider.GetRequiredService<GmailReaderService>();
        var gemini = scope.ServiceProvider.GetRequiredService<GeminiService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _logger.LogInformation("Polling Gmail...");

        var emails = await gmail.GetRecentEmailsAsync();
        _logger.LogInformation("Found {Count} email(s) in the last day", emails.Count);

        var alreadyProcessed = await db.ProcessedEmails.Select(p => p.GmailMessageId).ToListAsync();

        foreach (var (id, subject, from, body) in emails)
        {
            if (alreadyProcessed.Contains(id))
            {
                _logger.LogInformation("Skipping already-processed email: {Subject}", subject);
                continue;
            }

            try
            {
                await ProcessOneEmail(id, subject, from, body, gemini, db);
            }
            catch (Exception ex)
            {
                // One bad email should never take down the rest of the batch,
                // and should NOT be marked processed — it'll be retried next poll.
                _logger.LogError(ex, "Failed to process email: {Subject}", subject);
            }
        }
    }

    private async Task ProcessOneEmail(string id, string subject, string from, string body,
        GeminiService gemini, AppDbContext db)
    {
        _logger.LogInformation("Classifying new email: {Subject}", subject);
        var result = await gemini.ClassifyEmailAsync(subject, from, body);
        _logger.LogInformation("Result — jobRelated: {JobRelated}, company: {Company}, status: {Status}",
            result.JobRelated, result.Company, result.Status);

        // Only mark as processed once classification itself has succeeded.
        db.ProcessedEmails.Add(new ProcessedEmail { GmailMessageId = id });
        await db.SaveChangesAsync();

        if (!result.JobRelated || result.Company == null) return;

        var newStatus = result.Status switch
        {
            "application_confirmation" => "applied",
            "interview" => "interview",
            "offer" => "offer",
            "rejected" => "rejected",
            "no_response_but_active" => "applied",
            _ => null
        };

        if (newStatus == null) return;

        var match = await db.Applications
            .Where(a => EF.Functions.ILike(a.Company, $"%{result.Company}%"))
            .FirstOrDefaultAsync();

        if (match == null)
        {
            db.Applications.Add(new JobApplication
            {
                Company = result.Company,
                Role = result.Role ?? "",
                DateApplied = DateOnly.FromDateTime(DateTime.Today),
                Status = newStatus,
                LastUpdated = DateTime.UtcNow,
                Summary = newStatus is "rejected" or "offer" ? result.Summary : null
            });
            await db.SaveChangesAsync();
            _logger.LogInformation("Auto-created application for {Company} with status {Status}", result.Company, newStatus);
            return;
        }

        if (match.Status is "offer" or "rejected")
        {
            _logger.LogInformation("Skipping {Company} — already at final status {Status}", match.Company, match.Status);
            return;
        }

        if (newStatus != match.Status)
        {
            match.Status = newStatus;
            match.LastUpdated = DateTime.UtcNow;
            match.NudgeAcknowledged = false;
            if (newStatus is "rejected" or "offer")
                match.Summary = result.Summary;
            await db.SaveChangesAsync();
            _logger.LogInformation("Updated {Company} to {Status}", match.Company, newStatus);
        }
    }
}
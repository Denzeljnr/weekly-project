using WriteAngle.Interfaces;
using WriteAngle.Models;
using System.Text.Json;

namespace WriteAngle.Services;

public class ReportOrchestrator : IReportOrchestrator
{
    private const int MaxRevisions = 2;

    private readonly IResearchAgent _researcher;
    private readonly ICriticAgent _critic;
    private readonly IWriterAgent _writer;
    private readonly IReportRepository _repository;

    public ReportOrchestrator(IResearchAgent researcher, ICriticAgent critic, IWriterAgent writer, IReportRepository repository)
    {
        _researcher = researcher;
        _critic = critic;
        _writer = writer;
        _repository = repository;
    }

    public async Task<Report> GenerateReportAsync(string topic)
    {
        var draft = await _researcher.ResearchAsync(topic);
        var revisionCount = 0;

        while (revisionCount < MaxRevisions)
        {
            var critique = await _critic.ReviewAsync(draft);
            if (critique.Approved) break;

            draft = await _researcher.ResearchAsync(topic, critique.Issues);
            revisionCount++;
        }

        var finalText = await _writer.WriteAsync(topic, draft);

        var report = new Report
        {
            Topic = topic,
            FinalText = finalText,
            RevisionCount = revisionCount,
            SourcesJson = JsonSerializer.Serialize(draft.Claims)
        };

        await _repository.SaveAsync(report);
        return report;
    }
}
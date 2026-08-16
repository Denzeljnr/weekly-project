using System.Text;
using System.Text.Json;
using IncidentAgent.Services;

namespace IncidentAgent.Services;

public record Diagnosis(string Summary, string LikelyCause, string Evidence, string AffectedService, string RecommendedAction);

public interface IDiagnosisService
{
    Task<Diagnosis> DiagnoseAsync(CorrelatedEvidence evidence);
}

public class DiagnosisService : IDiagnosisService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    // No change needed here even though DiagnosisService now implements an interface —
    // AddHttpClient<IDiagnosisService, DiagnosisService>() still injects a real HttpClient
    // into the concrete class's constructor exactly as before; the interface only changes
    // how callers *resolve* this service, not how it's constructed internally.
    public DiagnosisService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<Diagnosis> DiagnoseAsync(CorrelatedEvidence evidence)
    {
        var apiKey = _config["Gemini:ApiKey"];

        var systemPrompt = @"You are an incident response assistant. You are given correlated evidence
around a triggered alert: recent error logs, recent deployments, and recent database events.
Your job is to produce a root-cause diagnosis.

Respond ONLY in this exact JSON format, no other text:
{
  ""summary"": ""one line describing what happened, e.g. 'API response time increased by 340%'"",
  ""likely_cause"": ""your best hypothesis for the root cause"",
  ""evidence"": ""a short explanation citing specific evidence (timestamps, commit shas, event types) that supports the hypothesis"",
  ""affected_service"": ""which endpoint/service this affects, based on the evidence"",
  ""recommended_action"": ""a specific, actionable recommendation, e.g. 'roll back deployment X' or 'increase connection pool size'""
}";

        var evidenceText = new StringBuilder();
        evidenceText.AppendLine($"Alert: {evidence.Alert.Message} (fired at {evidence.Alert.FiredAt:HH:mm:ss})");
        evidenceText.AppendLine($"Computed confidence: {evidence.ConfidenceScore}% ({evidence.RelatedEventsCount} related events)");
        evidenceText.AppendLine("\nRecent deployments:");
        foreach (var d in evidence.RecentDeployments)
            evidenceText.AppendLine($"- {d.CommitSha} at {d.DeployedAt:HH:mm:ss}: {d.Description}");
        evidenceText.AppendLine("\nRecent error logs:");
        foreach (var l in evidence.RecentLogs)
            evidenceText.AppendLine($"- {l.CreatedAt:HH:mm:ss} [{l.Endpoint}] {l.Message} (response time: {l.ResponseTimeMs}ms)");
        evidenceText.AppendLine("\nRecent DB events:");
        foreach (var e in evidence.RecentDbEvents)
            evidenceText.AppendLine($"- {e.CreatedAt:HH:mm:ss} {e.EventType}: {e.Detail} ({e.DurationMs}ms)");

        var requestBody = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = $"{systemPrompt}\n\n{evidenceText}" } } }
            }
        };

        var res = await _http.PostAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}",
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        );

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Gemini API error: {res.StatusCode} {await res.Content.ReadAsStringAsync()}");

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";

        var cleaned = text.Replace("```json", "").Replace("```", "").Trim();
        var parsed = JsonSerializer.Deserialize<JsonElement>(cleaned);

        return new Diagnosis(
            parsed.GetProperty("summary").GetString() ?? "",
            parsed.GetProperty("likely_cause").GetString() ?? "",
            parsed.GetProperty("evidence").GetString() ?? "",
            parsed.GetProperty("affected_service").GetString() ?? "",
            parsed.GetProperty("recommended_action").GetString() ?? ""
        );
    }
}
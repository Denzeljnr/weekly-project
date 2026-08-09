using System.Text;
using System.Text.Json;

namespace JobTracker.Services;

public record ClassificationResult(bool JobRelated, string? Company, string? Role, string? Status, string? Summary);

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<ClassificationResult> ClassifyEmailAsync(string subject, string from, string body)
    {
        var apiKey = _config["Gemini:ApiKey"];
        var systemPrompt = @"You are classifying inbound emails for a job application tracker.
Given an email's subject, sender, and body, determine:
1. Is this related to a job application? (yes/no)
2. What's the company name? Extract it cleanly (e.g. 'Google', not 'Google LLC Careers Team').
3. What's the job title/role, if mentioned? Null if not mentioned.
4. Classify status as exactly one of:
   - application_confirmation (first automated 'thanks for applying' email, confirming a NEW application)
   - interview (an interview is being requested or scheduled)
   - offer (a job offer is being extended, or acceptance of the candidate is confirmed)
   - rejected (the application was declined)
   - no_response_but_active (a follow-up about an existing application, no new outcome)
   - unclear (job-related but doesn't fit any of the above confidently)
5. If status is 'rejected' or 'offer', write a single plain-language sentence summarizing the email's key point (e.g. reason given, next steps, salary/start date if an offer). Otherwise null.

Respond ONLY in this exact JSON format, no other text:
{""job_related"": true or false, ""company"": ""string or null"", ""role"": ""string or null"", ""status"": ""string or null"", ""summary"": ""string or null""}";

        var userPrompt = $"Subject: {subject}\nFrom: {from}\nBody: {body}";

        var requestBody = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = $"{systemPrompt}\n\n{userPrompt}" } } }
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

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(cleaned);
            return new ClassificationResult(
                parsed.GetProperty("job_related").GetBoolean(),
                parsed.TryGetProperty("company", out var c) ? c.GetString() : null,
                parsed.TryGetProperty("role", out var r) ? r.GetString() : null,
                parsed.TryGetProperty("status", out var s) ? s.GetString() : null,
                parsed.TryGetProperty("summary", out var sm) ? sm.GetString() : null
            );
        }
        catch
        {
            return new ClassificationResult(false, null, null, null, null);
        }
    }
}
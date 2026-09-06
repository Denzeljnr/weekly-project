using System.Text;
using System.Text.Json;
using WriteAngle.Interfaces;

namespace WriteAngle.Services.Gemini;

public class GeminiTextClient : IGeminiTextClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GeminiTextClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, bool groundedSearch = false)
    {
        var apiKey = _config["Gemini:ApiKey"];

        var bodyObj = new Dictionary<string, object>
        {
            ["contents"] = new[] { new { role = "user", parts = new[] { new { text = $"{systemPrompt}\n\n{userPrompt}" } } } }
        };

        // this one flag is the entire "grounding" integration — no separate search API involved
        if (groundedSearch)
            bodyObj["tools"] = new[] { new { google_search = new { } } };

        var res = await _http.PostAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}",
            new StringContent(JsonSerializer.Serialize(bodyObj), Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Gemini API error: {res.StatusCode} {await res.Content.ReadAsStringAsync()}");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
    }
}
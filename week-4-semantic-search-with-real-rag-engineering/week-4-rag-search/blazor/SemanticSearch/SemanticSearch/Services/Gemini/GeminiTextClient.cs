using System.Text;
using System.Text.Json;
using SemanticSearch.Interfaces;

namespace SemanticSearch.Services.Gemini;

// the ONE place that knows how to send a prompt to Gemini and parse the text back out.
// RerankingService and AnswerService both use this instead of each writing their own HTTP call.
public class GeminiTextClient : IGeminiTextClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GeminiTextClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        var apiKey = _config["Gemini:ApiKey"];
        var body = new { contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } } };

        var res = await _http.PostAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Gemini API error: {res.StatusCode} {await res.Content.ReadAsStringAsync()}");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
    }
}
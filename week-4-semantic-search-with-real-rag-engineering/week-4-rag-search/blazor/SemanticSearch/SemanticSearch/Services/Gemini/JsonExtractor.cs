using System.Text.Json;

namespace SemanticSearch.Services.Gemini;

// Gemini occasionally wraps JSON output in ```json fences despite being told not to.
// One place strips that, instead of every consumer defensively re-implementing the same fix.
public static class JsonExtractor
{
    public static T? Parse<T>(string raw)
    {
        var cleaned = raw.Replace("```json", "").Replace("```", "").Trim();
        try { return JsonSerializer.Deserialize<T>(cleaned); }
        catch { return default; }
    }
}
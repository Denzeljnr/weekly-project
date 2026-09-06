using System.Text.Json;

namespace WriteAngle.Services.Gemini;

public static class JsonExtractor
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T? Parse<T>(string raw)
    {
        var cleaned = raw.Replace("```json", "").Replace("```", "").Trim();
        try { return JsonSerializer.Deserialize<T>(cleaned, Options); }
        catch { return default; }
    }
}
using System.Text;
using System.Text.Json;
using Pgvector;
using SemanticSearch.Interfaces;

namespace SemanticSearch.Services.Gemini;

public class GeminiEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly EmbeddingRateLimiter _rateLimiter;

    public GeminiEmbeddingClient(HttpClient http, IConfiguration config, EmbeddingRateLimiter rateLimiter)
    {
        _http = http;
        _config = config;
        _rateLimiter = rateLimiter;
    }

    public async Task<Vector> EmbedAsync(string text)
    {
        await _rateLimiter.WaitForSlotAsync(1);

        var apiKey = _config["Gemini:ApiKey"];
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={apiKey}";
        var body = new
        {
            content = new { parts = new[] { new { text } } },
            outputDimensionality = 768
        };

        var res = await _http.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Embedding API error: {res.StatusCode} {await res.Content.ReadAsStringAsync()}");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var values = doc.RootElement.GetProperty("embedding").GetProperty("values")
            .EnumerateArray().Select(v => v.GetSingle()).ToArray();

        return new Vector(values);
    }

    public async Task<List<Vector>> EmbedBatchAsync(List<string> texts)
    {
        // Each text in the batch counts as one request against the free-tier quota,
        // so wait for enough "slots" before sending, not just one per HTTP call.
        await _rateLimiter.WaitForSlotAsync(texts.Count);

        var apiKey = _config["Gemini:ApiKey"];
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:batchEmbedContents?key={apiKey}";

        var requests = texts.Select(t => new
        {
            model = "models/gemini-embedding-001",
            content = new { parts = new[] { new { text = t } } },
            outputDimensionality = 768
        });
        var body = new { requests };

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var res = await _http.PostAsync(url,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

            if (res.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                return doc.RootElement.GetProperty("embeddings")
                    .EnumerateArray()
                    .Select(e => new Vector(e.GetProperty("values").EnumerateArray().Select(v => v.GetSingle()).ToArray()))
                    .ToList();
            }

            var status = (int)res.StatusCode;
            if ((status == 429 || status == 503) && attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt) * 2)); // 4s, 8s, 16s — longer, since 429 needs real cooldown
                continue;
            }

            throw new Exception($"Batch embedding API error: {res.StatusCode} {await res.Content.ReadAsStringAsync()}");
        }

        throw new Exception("Batch embedding failed after retries.");
    }
}
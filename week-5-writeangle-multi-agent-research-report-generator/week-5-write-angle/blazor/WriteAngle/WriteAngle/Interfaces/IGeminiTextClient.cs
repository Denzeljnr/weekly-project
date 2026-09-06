namespace WriteAngle.Interfaces;

public interface IGeminiTextClient
{
    // groundedSearch = true enables Gemini's built-in Google Search tool for this call
    Task<string> GenerateAsync(string systemPrompt, string userPrompt, bool groundedSearch = false);
}
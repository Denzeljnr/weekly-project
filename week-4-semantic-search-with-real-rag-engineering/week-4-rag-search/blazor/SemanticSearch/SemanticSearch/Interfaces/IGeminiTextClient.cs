namespace SemanticSearch.Interfaces;

// wraps any Gemini call that sends a prompt and gets text back —
// shared by reranking and answer generation, which both do exactly this
public interface IGeminiTextClient
{
    Task<string> GenerateAsync(string prompt);
}
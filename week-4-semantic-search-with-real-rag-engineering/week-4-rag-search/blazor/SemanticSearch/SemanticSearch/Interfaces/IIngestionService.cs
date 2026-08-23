using SemanticSearch.Models;

namespace SemanticSearch.Interfaces;

public interface IIngestionService
{
    Task IngestAsync(string sourceDocument, string fullText, IProgress<IngestionProgress>? progress = null);
}
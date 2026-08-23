using SemanticSearch.Models;

namespace SemanticSearch.Interfaces;

public interface IDocumentService
{
    Task<PagedDocuments> ListDocumentsAsync(int page = 1, int pageSize = 5);
    Task DeleteDocumentAsync(string sourceDocument);
}
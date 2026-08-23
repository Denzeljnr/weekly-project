namespace SemanticSearch.Models;

public record DocumentSummary(string SourceDocument, int ChunkCount);

public record PagedDocuments(List<DocumentSummary> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
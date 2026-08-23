using Microsoft.EntityFrameworkCore;
using SemanticSearch.Data;
using SemanticSearch.Interfaces;
using SemanticSearch.Models;

namespace SemanticSearch.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;

    public DocumentService(AppDbContext db) => _db = db;

    public async Task<PagedDocuments> ListDocumentsAsync(int page = 1, int pageSize = 5)
    {
        var grouped = _db.Chunks
            .GroupBy(c => c.SourceDocument)
            .OrderBy(g => g.Key);

        var totalCount = await grouped.CountAsync();

        var items = await grouped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new DocumentSummary(g.Key, g.Count()))
            .ToListAsync();

        return new PagedDocuments(items, totalCount, page, pageSize);
    }

    public async Task DeleteDocumentAsync(string sourceDocument)
    {
        await _db.Chunks
            .Where(c => c.SourceDocument == sourceDocument)
            .ExecuteDeleteAsync();
    }
}
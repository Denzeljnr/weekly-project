using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using SemanticSearch.Data;
using SemanticSearch.Interfaces;
using SemanticSearch.Models;

namespace SemanticSearch.Services;

public class RetrievalService : IRetrievalService
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingClient _embedder;

    public RetrievalService(AppDbContext db, IEmbeddingClient embedder)
    {
        _db = db;
        _embedder = embedder;
    }

    public async Task<List<Chunk>> RetrieveCandidatesAsync(string question, string strategy, int take = 20)
    {
        var queryEmbedding = await _embedder.EmbedAsync(question);

        return await _db.Chunks
            .Where(c => c.ChunkingStrategy == strategy)
            .OrderBy(c => c.Embedding.L2Distance(queryEmbedding))
            .Take(take)
            .ToListAsync();
    }
}
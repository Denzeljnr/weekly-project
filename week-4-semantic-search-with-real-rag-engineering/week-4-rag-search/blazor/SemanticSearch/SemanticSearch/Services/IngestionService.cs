using SemanticSearch.Data;
using SemanticSearch.Interfaces;
using SemanticSearch.Models;

namespace SemanticSearch.Services;

public class IngestionService : IIngestionService
{
    private readonly IEnumerable<IChunker> _chunkers;
    private readonly IEmbeddingClient _embedder;
    private readonly AppDbContext _db;

    private const int BatchSize = 50;        // chunks per Gemini API call
    private const int MaxConcurrentBatches = 3; // simultaneous in-flight batches

    public IngestionService(IEnumerable<IChunker> chunkers, IEmbeddingClient embedder, AppDbContext db)
    {
        _chunkers = chunkers;
        _embedder = embedder;
        _db = db;
    }

    public async Task IngestAsync(string sourceDocument, string fullText, IProgress<IngestionProgress>? progress = null)
    {
        // Flatten every (strategy, chunkText) pair across all chunkers up front,
        // so we know the true total for progress reporting before any API calls happen.
        var pending = new List<(string Strategy, string Text)>();
        foreach (var chunker in _chunkers)
            foreach (var text in chunker.Chunk(fullText))
                pending.Add((chunker.StrategyName, text));

        progress?.Report(new IngestionProgress(0, pending.Count, "Embedding"));

        var batches = pending
            .Select((item, index) => (item, index))
            .GroupBy(x => x.index / BatchSize)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();

        int completed = 0;
        var semaphore = new SemaphoreSlim(MaxConcurrentBatches);

        // Process in groups sized to the concurrency limit, so we can safely
        // save to the DbContext (not thread-safe) between groups rather than mid-flight.
        for (int i = 0; i < batches.Count; i += MaxConcurrentBatches)
        {
            var group = batches.Skip(i).Take(MaxConcurrentBatches).ToList();

            var groupTasks = group.Select(async batch =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var vectors = await _embedder.EmbedBatchAsync(batch.Select(b => b.Text).ToList());
                    return batch.Zip(vectors, (b, v) => new Chunk
                    {
                        SourceDocument = sourceDocument,
                        Content = b.Text,
                        ChunkingStrategy = b.Strategy,
                        Embedding = v
                    }).ToList();
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var groupResults = await Task.WhenAll(groupTasks);

            foreach (var chunkList in groupResults)
            {
                _db.Chunks.AddRange(chunkList);
                completed += chunkList.Count;
            }

            // Save after each group — if a later group fails, everything ingested
            // so far is already persisted instead of being lost.
            await _db.SaveChangesAsync();
            progress?.Report(new IngestionProgress(completed, pending.Count, "Embedding"));
        }

        progress?.Report(new IngestionProgress(pending.Count, pending.Count, "Done"));
    }
}
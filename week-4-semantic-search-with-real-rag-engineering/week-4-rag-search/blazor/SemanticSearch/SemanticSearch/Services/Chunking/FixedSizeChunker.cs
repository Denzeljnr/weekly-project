using SemanticSearch.Interfaces;

namespace SemanticSearch.Services.Chunking;

// naive: cuts every N characters, no awareness of sentence or paragraph structure
public class FixedSizeChunker : IChunker
{
    public string StrategyName => "fixed";
    private readonly int _size;

    public FixedSizeChunker(int size = 500) => _size = size;

    public List<string> Chunk(string text)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += _size)
            chunks.Add(text.Substring(i, Math.Min(_size, text.Length - i)));
        return chunks;
    }
}
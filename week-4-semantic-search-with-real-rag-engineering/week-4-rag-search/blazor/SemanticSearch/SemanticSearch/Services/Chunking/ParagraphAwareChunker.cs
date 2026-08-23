using SemanticSearch.Interfaces;

namespace SemanticSearch.Services.Chunking;

// respects natural boundaries — never splits a paragraph mid-sentence,
// at the cost of uneven chunk sizes
public class ParagraphAwareChunker : IChunker
{
    public string StrategyName => "paragraph";
    private readonly int _maxSize;

    public ParagraphAwareChunker(int maxSize = 500) => _maxSize = maxSize;

    public List<string> Chunk(string text)
    {
        var paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var current = "";

        foreach (var para in paragraphs)
        {
            if ((current + para).Length > _maxSize && current.Length > 0)
            {
                chunks.Add(current.Trim());
                current = "";
            }
            current += para + "\n\n";
        }
        if (current.Trim().Length > 0) chunks.Add(current.Trim());
        return chunks;
    }
}
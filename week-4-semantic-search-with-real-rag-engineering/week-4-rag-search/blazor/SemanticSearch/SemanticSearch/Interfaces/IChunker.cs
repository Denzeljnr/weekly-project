namespace SemanticSearch.Interfaces;

public interface IChunker
{
    // a short identifier used to tag which strategy produced a chunk — e.g. "fixed", "paragraph"
    string StrategyName { get; }
    List<string> Chunk(string text);
}
using SemanticSearch.Models;

namespace SemanticSearch.Interfaces;

public interface IRetrievalService
{
    Task<List<Chunk>> RetrieveCandidatesAsync(string question, string strategy, int take = 20);
}
using WriteAngle.Models;

namespace WriteAngle.Interfaces;

public interface ICriticAgent
{
    Task<CritiqueResult> ReviewAsync(ResearchDraft draft);
}
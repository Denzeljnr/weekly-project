namespace WriteAngle.Models;

public record CritiqueResult(bool Approved, List<CritiqueIssue> Issues);
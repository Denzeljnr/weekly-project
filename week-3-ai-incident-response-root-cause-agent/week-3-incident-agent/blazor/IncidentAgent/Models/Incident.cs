namespace IncidentAgent.Models;

public class Incident
{
    public int Id { get; set; }
    public int AlertId { get; set; }
    public string Summary { get; set; } = "";
    public string LikelyCause { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string AffectedService { get; set; } = "";
    public int ConfidenceScore { get; set; }
    public string RecommendedAction { get; set; } = "";
    public int RelatedEventsCount { get; set; }
    public string Source { get; set; } = "blazor";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
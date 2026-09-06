namespace WriteAngle.Models;

public class Report
{
    public int Id { get; set; }
    public string Topic { get; set; } = "";
    public string FinalText { get; set; } = "";
    public int RevisionCount { get; set; }
    public string SourcesJson { get; set; } = "[]"; // serialized List<SourcedClaim>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
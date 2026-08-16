namespace IncidentAgent.Models;

public class LogEntry
{
    public int Id { get; set; }
    public string Level { get; set; } = "";
    public string? Endpoint { get; set; }
    public string? Message { get; set; }
    public int? ResponseTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Deployment
{
    public int Id { get; set; }
    public string CommitSha { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DeployedAt { get; set; }
}

public class DbEvent
{
    public int Id { get; set; }
    public string? EventType { get; set; }
    public string? Detail { get; set; }
    public int? DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Alert
{
    public int Id { get; set; }
    public string? Metric { get; set; }
    public decimal? ThresholdValue { get; set; }
    public decimal? ActualValue { get; set; }
    public string? Message { get; set; }
    public DateTime FiredAt { get; set; }
}
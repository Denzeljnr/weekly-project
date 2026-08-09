namespace JobTracker.Models;

public class JobApplication
{
    public int Id { get; set; }
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public DateOnly DateApplied { get; set; }
    public string Status { get; set; } = "applied"; // applied, interview, offer, rejected
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string? Summary { get; set; }
    public bool NudgeAcknowledged { get; set; } = false;
}
namespace JobTracker.Models;

public class ProcessedEmail
{
    public int Id { get; set; }
    public string GmailMessageId { get; set; } = "";
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
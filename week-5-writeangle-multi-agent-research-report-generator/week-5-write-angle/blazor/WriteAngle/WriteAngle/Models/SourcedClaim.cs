namespace WriteAngle.Models;

// one factual claim, tied to the source URL that's supposed to support it
public record SourcedClaim(string Claim, string SourceUrl, string SourceTitle);
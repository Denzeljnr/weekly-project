using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace JobTracker.Services;

public class GmailReaderService
{
    private GmailService? _service;

    private async Task<GmailService> GetServiceAsync()
    {
        if (_service != null) return _service;

        using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailReadonly },
            "user",
            CancellationToken.None,
            new FileDataStore("token.json", true)
        );

        _service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "JobTracker"
        });
        return _service;
    }

    public async Task<List<(string Id, string Subject, string From, string Body)>> GetRecentEmailsAsync(int maxResults = 10)
    {
        var service = await GetServiceAsync();
        var listRequest = service.Users.Messages.List("me");
        listRequest.MaxResults = maxResults;
        listRequest.Q = "newer_than:2d (application OR interview OR position OR role OR candidacy OR \"thank you for applying\" OR reject OR offer OR Unfortunately)";

        var listResponse = await listRequest.ExecuteAsync();
        var results = new List<(string, string, string, string)>();

        if (listResponse.Messages == null) return results;

        foreach (var msg in listResponse.Messages)
        {
            var full = await service.Users.Messages.Get("me", msg.Id).ExecuteAsync();
            var subject = full.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "";
            var from = full.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value ?? "";
            var body = ExtractBody(full.Payload);
            results.Add((msg.Id, subject, from, body));
        }
        return results;
    }

    private string ExtractBody(Google.Apis.Gmail.v1.Data.MessagePart payload)
    {
        if (payload.Body?.Data != null)
            return Base64UrlDecode(payload.Body.Data);

        if (payload.Parts != null)
        {
            var textPart = payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
            if (textPart?.Body?.Data != null)
                return Base64UrlDecode(textPart.Body.Data);
        }
        return "";
    }

    private string Base64UrlDecode(string input)
    {
        string s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
}
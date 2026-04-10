using System.Text;
using System.Text.Json;

namespace BinokelTracker.Services;

public class FeedbackService : IFeedbackService
{
    private readonly HttpClient _http;
    private const string FormspreeEndpoint = "https://formspree.io/f/xkoplyag";

    public FeedbackService(HttpClient http)
    {
        _http = http;
    }

    public Task<byte[]?> CaptureScreenshotAsync() => Task.FromResult<byte[]?>(null);

    public async Task SendFeedbackAsync(string comment, byte[]? screenshot)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var body = string.IsNullOrWhiteSpace(comment) ? "(Kein Kommentar)" : comment;

            var payload = new
            {
                _subject = $"[Binokel Tracker] Bug Report – {timestamp}",
                message = body,
                timestamp
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _http.PostAsync(FormspreeEndpoint, content);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SendFeedback failed: {ex.Message}");
        }
    }
}

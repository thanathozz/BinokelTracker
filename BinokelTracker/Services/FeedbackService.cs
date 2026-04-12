using System.Text;
using System.Text.Json;
using Microsoft.Maui.Media;

namespace BinokelTracker.Services;

public class FeedbackService : IFeedbackService
{
    private readonly HttpClient _http;
    private const string FormspreeEndpoint = "https://formspree.io/f/xkoplyag";

    public FeedbackService(HttpClient http)
    {
        _http = http;
    }

    public async Task<byte[]?> CaptureScreenshotAsync()
    {
        try
        {
            if (!Screenshot.Default.IsCaptureSupported)
                return null;

            var result = await Screenshot.Default.CaptureAsync();
            using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot failed: {ex.Message}");
            return null;
        }
    }

    public async Task SendFeedbackAsync(string comment, byte[]? screenshot)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var body = string.IsNullOrWhiteSpace(comment)
                ? "(Kein Kommentar)"
                : comment;

            if (screenshot is not null)
                body += "\n\n[Screenshot wurde aufgenommen]";

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

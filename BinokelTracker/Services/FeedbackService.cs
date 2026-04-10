using Microsoft.Maui.Media;
using Microsoft.Maui.ApplicationModel.Communication;
using BinokelTracker.Models;

namespace BinokelTracker.Services;

public class FeedbackService : IFeedbackService
{
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
            var subject = $"[Binokel Tracker] Bug Report – {timestamp}";
            var body = string.IsNullOrWhiteSpace(comment)
                ? "(Kein Kommentar)"
                : comment;

            var message = new EmailMessage
            {
                Subject = subject,
                Body = body,
                To = new List<string>(){"masterflyn@gmx.de"}
            };

            if (screenshot is not null)
            {
                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var path = Path.Combine(FileSystem.CacheDirectory, fileName);
                await File.WriteAllBytesAsync(path, screenshot);
                message.Attachments.Add(new EmailAttachment(path, "image/png"));
            }

            await Email.Default.ComposeAsync(message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SendFeedback failed: {ex.Message}");
        }
    }
}

namespace BinokelTracker.Services;

public interface IFeedbackService
{
    Task<byte[]?> CaptureScreenshotAsync();
    Task SendFeedbackAsync(string comment, byte[]? screenshot);
}

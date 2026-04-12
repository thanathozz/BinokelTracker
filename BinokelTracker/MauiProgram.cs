using Microsoft.Extensions.Logging;
namespace BinokelTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<Services.IGameService, Services.GameService>();
        builder.Services.AddHttpClient<Services.IGameStorageService, Services.SupabaseGameStorageService>();
        builder.Services.AddHttpClient<Services.IFeedbackService, Services.FeedbackService>();

#if DEBUG
    builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

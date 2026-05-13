using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
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

        var supabaseConfig = LoadSupabaseConfig();
        builder.Services.AddSingleton(supabaseConfig);

        var anthropicConfig = LoadAnthropicConfig();
        builder.Services.AddSingleton(anthropicConfig);
        builder.Services.AddSingleton<Services.IMeldScanService, Services.MeldScanService>();

        builder.Services.AddHttpClient("auth");
        builder.Services.AddScoped<Services.IAuthService>(sp =>
            new Services.AuthService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("auth"),
                sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(),
                sp.GetRequiredService<Services.SupabaseConfig>()));
        builder.Services.AddHttpClient<Services.IGameStorageService, Services.SupabaseGameStorageService>();
        builder.Services.AddHttpClient<Services.IFeedbackService, Services.FeedbackService>();
        builder.Services.AddScoped<BinokelTracker.Shared.Services.ThemeService>();

#if DEBUG
    builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static Services.SupabaseConfig LoadSupabaseConfig()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("appsettings.json");
        if (stream is null)
            throw new InvalidOperationException("appsettings.json nicht gefunden. Bitte appsettings.example.json kopieren.");

        using var doc = JsonDocument.Parse(stream);
        var section = doc.RootElement.GetProperty("Supabase");
        return new Services.SupabaseConfig
        {
            Url     = section.GetProperty("Url").GetString()     ?? throw new InvalidOperationException("Supabase:Url fehlt"),
            AnonKey = section.GetProperty("AnonKey").GetString() ?? throw new InvalidOperationException("Supabase:AnonKey fehlt"),
        };
    }

    private static Services.AnthropicConfig LoadAnthropicConfig()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("appsettings.json");
        if (stream is null) return new Services.AnthropicConfig();

        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("Anthropic", out var section)) return new Services.AnthropicConfig();
        return new Services.AnthropicConfig
        {
            ApiKey = section.TryGetProperty("ApiKey", out var k) ? k.GetString() ?? "" : "",
        };
    }
}

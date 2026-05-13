using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BinokelTracker.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<BinokelTracker.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var supabaseConfig = new SupabaseConfig
{
    Url     = builder.Configuration["Supabase:Url"]     ?? throw new InvalidOperationException("Supabase:Url nicht konfiguriert"),
    AnonKey = builder.Configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase:AnonKey nicht konfiguriert"),
};
builder.Services.AddSingleton(supabaseConfig);

builder.Services.AddHttpClient("auth");
builder.Services.AddScoped<IAuthService>(sp =>
    new AuthService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("auth"),
        sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(),
        sp.GetRequiredService<SupabaseConfig>()));
builder.Services.AddHttpClient<IGameStorageService, SupabaseGameStorageService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<BinokelTracker.Shared.Services.ThemeService>();
var anthropicConfig = new BinokelTracker.Services.AnthropicConfig
{
    ApiKey = builder.Configuration["Anthropic:ApiKey"] ?? ""
};
builder.Services.AddSingleton(anthropicConfig);
builder.Services.AddSingleton<BinokelTracker.Services.IMeldScanService, BinokelTracker.Web.Services.MeldScanService>();

await builder.Build().RunAsync();

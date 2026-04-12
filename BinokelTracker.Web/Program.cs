using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BinokelTracker.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<BinokelTracker.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("auth");
builder.Services.AddScoped<IAuthService>(sp =>
    new AuthService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("auth"),
        sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
builder.Services.AddHttpClient<IGameStorageService, SupabaseGameStorageService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<BinokelTracker.Shared.Services.ThemeService>();

await builder.Build().RunAsync();

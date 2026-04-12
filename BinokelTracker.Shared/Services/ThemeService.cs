using Microsoft.JSInterop;

namespace BinokelTracker.Shared.Services;

public class ThemeService
{
    private readonly IJSRuntime _js;

    public string CurrentTheme { get; private set; } = "braun";

    public event Action? ThemeChanged;

    public ThemeService(IJSRuntime js) => _js = js;

    public async Task LoadAsync()
    {
        CurrentTheme = await _js.InvokeAsync<string>("BinokelTheme.load");
        ThemeChanged?.Invoke();
    }

    public async Task ToggleAsync()
    {
        var next = CurrentTheme == "braun" ? "grau" : "braun";
        CurrentTheme = next;
        await _js.InvokeVoidAsync("BinokelTheme.apply", next);
        ThemeChanged?.Invoke();
    }
}

using Microsoft.JSInterop;

namespace BinokelTracker.Shared.Services;

public class LanguageService
{
    private readonly IJSRuntime _js;

    public string CurrentLang { get; private set; } = "de";
    public IStrings Current   { get; private set; } = new StringsDe();

    public event Action? LanguageChanged;

    public LanguageService(IJSRuntime js) => _js = js;

    public async Task LoadAsync()
    {
        CurrentLang = await _js.InvokeAsync<string>("BinokelLang.load");
        Current = Build(CurrentLang);
        LanguageChanged?.Invoke();
    }

    public async Task SetAsync(string lang)
    {
        CurrentLang = lang;
        Current = Build(lang);
        await _js.InvokeVoidAsync("BinokelLang.save", lang);
        LanguageChanged?.Invoke();
    }

    private static IStrings Build(string lang) => lang switch
    {
        "en"      => new StringsEn(),
        "swabian" => new StringsSwabian(),
        _         => new StringsDe()
    };
}

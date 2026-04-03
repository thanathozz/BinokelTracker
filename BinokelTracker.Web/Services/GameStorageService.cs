using Blazored.LocalStorage;
using BinokelTracker.Models;

namespace BinokelTracker.Services;

public class GameStorageService : IGameStorageService
{
    private readonly ILocalStorageService _storage;
    private const string StorageKey = "binokel-data-v2";

    public GameStorageService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public async Task<AppState> LoadAsync()
    {
        try
        {
            return await _storage.GetItemAsync<AppState>(StorageKey) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public async Task SaveAsync(AppState state)
    {
        try
        {
            await _storage.SetItemAsync(StorageKey, state);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
        }
    }
}

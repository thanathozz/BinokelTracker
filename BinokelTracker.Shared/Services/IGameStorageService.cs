using BinokelTracker.Models;

namespace BinokelTracker.Services;

public interface IGameStorageService
{
    Task<AppState> LoadAsync();
    Task<string?> SaveAsync(AppState state);
}

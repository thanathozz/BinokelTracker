using BinokelTracker.Models;

namespace BinokelTracker.Services;

public interface IGameStorageService
{
    Task<AppState> LoadAsync();
    Task<string?> SaveAsync(AppState state);
    Task<(string UserId, string DisplayName)?> FindProfileByDisplayNameAsync(string displayName);
    Task AddSpielrundeMembersAsync(long spielrundeId, IEnumerable<(string UserId, string DisplayName)> members);
    Task DeleteSpielrundeAsync(long spielrundeId);
}

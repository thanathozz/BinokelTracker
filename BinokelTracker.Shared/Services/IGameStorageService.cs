using BinokelTracker.Models;

namespace BinokelTracker.Services;

public interface IGameStorageService
{
    Task<AppState> LoadAsync();
    Task<string?> SaveAsync(AppState state);
    Task<(string UserId, string Nick)?> FindProfileByNickAsync(string nick);
    Task AddSpielrundeMembersAsync(long spielrundeId, IEnumerable<(string UserId, string Nick)> members);
}

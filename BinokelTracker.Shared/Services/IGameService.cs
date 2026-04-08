using BinokelTracker.Models;

namespace BinokelTracker.Services;

public interface IGameService
{
    void RegisterNewPlayers(AppState state, IEnumerable<string> playerNames);
    void AddRound(Game game, Round round);
    void DeleteRound(Game game, int roundIndex);
    void FinishGame(Game game);
}

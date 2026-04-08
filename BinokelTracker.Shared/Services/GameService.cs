using BinokelTracker.Models;

namespace BinokelTracker.Services;

public class GameService : IGameService
{
    public void RegisterNewPlayers(AppState state, IEnumerable<string> playerNames)
    {
        foreach (var name in playerNames)
        {
            if (!string.IsNullOrWhiteSpace(name) &&
                !state.KnownPlayers.Contains(name, StringComparer.OrdinalIgnoreCase))
                state.KnownPlayers.Add(name);
        }
    }

    public void AddRound(Game game, Round round)
    {
        game.Rounds.Add(round);
        if (!game.Finished && IsTargetReached(game))
            game.Finished = true;
    }

    public void DeleteRound(Game game, int roundIndex)
    {
        if (roundIndex >= 0 && roundIndex < game.Rounds.Count)
            game.Rounds.RemoveAt(roundIndex);
    }

    public void FinishGame(Game game) => game.Finished = true;

    private static bool IsTargetReached(Game game) => game.Rules.TeamMode
        ? ScoringCalculator.GetTeamTotals(game).Any(t => t >= game.Rules.TargetScore)
        : ScoringCalculator.GetPlayerTotals(game).Any(t => t >= game.Rules.TargetScore);
}

using System.Globalization;
using BinokelTracker.Models;

namespace BinokelTracker.Services;

public record GlobalStats(
    int TotalGames,
    int FinishedGames,
    int TotalRounds,
    IReadOnlyList<PlayerStats> Players);

public record PlayerStats(
    string Name,
    int GamesPlayed,
    int GamesWon,
    int RoundsAsBidder,
    int BidderWins,
    int BidderAbgegangen,
    int BidderLosses,
    int TotalBidSum,
    int BidCountNotAbg,
    int DurchPlayed,
    int DurchWon,
    int BettelPlayed,
    int BettelWon,
    decimal MoneyBalance)
{
    public double BidderWinRate  => RoundsAsBidder > 0 ? (double)BidderWins       / RoundsAsBidder * 100 : 0;
    public double AbgegangenRate => RoundsAsBidder > 0 ? (double)BidderAbgegangen / RoundsAsBidder * 100 : 0;
    public double AvgBidValue    => BidCountNotAbg > 0  ? (double)TotalBidSum      / BidCountNotAbg       : 0;
}

public static class StatisticsService
{
    public static GlobalStats Compute(IReadOnlyList<Game> games)
    {
        var builders = new Dictionary<string, Builder>(StringComparer.Ordinal);

        Builder Get(string name)
        {
            if (!builders.TryGetValue(name, out var b))
                builders[name] = b = new Builder(name);
            return b;
        }

        int totalRounds = 0;

        foreach (var game in games)
        {
            totalRounds += game.Rounds.Count;

            var winnerIndices = GetWinnerIndices(game);

            // Per-player game counters
            for (int pi = 0; pi < game.Players.Count; pi++)
            {
                var b = Get(game.Players[pi]);
                b.GamesPlayed++;
                if (winnerIndices.Contains(pi)) b.GamesWon++;
            }

            // Money balance
            AccumulateMoney(game, winnerIndices, builders);

            // Per-round counters
            foreach (var round in game.Rounds)
            {
                if (round.Bidder >= game.Players.Count) continue;
                var b = Get(game.Players[round.Bidder]);

                if (round.Type == RoundType.Durch)
                {
                    b.DurchPlayed++;
                    if (round.Won) b.DurchWon++;
                }
                else if (round.Type == RoundType.Bettel)
                {
                    b.BettelPlayed++;
                    if (round.Won) b.BettelWon++;
                }
                else
                {
                    b.RoundsAsBidder++;
                    b.TotalBidSum   += round.Bid;
                    b.BidCountNotAbg++;
                    if (round.Abgegangen) b.BidderAbgegangen++;
                    else if (round.Won)   b.BidderWins++;
                    else                  b.BidderLosses++;
                }
            }
        }

        var players = builders.Values
            .Select(b => b.Build())
            .OrderByDescending(p => p.MoneyBalance)
            .ThenByDescending(p => p.GamesWon)
            .ThenByDescending(p => p.GamesPlayed)
            .ThenBy(p => p.Name)
            .ToList();

        return new GlobalStats(
            TotalGames: games.Count,
            FinishedGames: games.Count(g => g.Finished),
            TotalRounds: totalRounds,
            Players: players);
    }

    /// <summary>
    /// Net gain/loss per player for a finished game.
    /// Winner earns Einsatz from each loser. Loser pays Einsatz once.
    /// </summary>
    public static decimal GameNetGain(Game game, int playerIndex)
    {
        if (!game.Finished) return 0;
        if (!decimal.TryParse(game.Einsatz, NumberStyles.Any, CultureInfo.InvariantCulture, out var einsatz) || einsatz == 0)
            return 0;

        var winners = GetWinnerIndices(game);
        if (winners.Count == 0) return 0;

        int losers = game.Players.Count - winners.Count;
        return winners.Contains(playerIndex)
            ? einsatz * losers / winners.Count   // winners split the losers' payments
            : -einsatz;
    }

    private static void AccumulateMoney(Game game, HashSet<int> winnerIndices, Dictionary<string, Builder> builders)
    {
        if (!game.Finished) return;
        if (!decimal.TryParse(game.Einsatz, NumberStyles.Any, CultureInfo.InvariantCulture, out var einsatz) || einsatz == 0)
            return;
        if (winnerIndices.Count == 0) return;

        int losers = game.Players.Count - winnerIndices.Count;

        for (int i = 0; i < game.Players.Count; i++)
        {
            if (!builders.TryGetValue(game.Players[i], out var b)) continue;
            b.MoneyBalance += winnerIndices.Contains(i)
                ? einsatz * losers / winnerIndices.Count
                : -einsatz;
        }
    }

    private static HashSet<int> GetWinnerIndices(Game game)
    {
        if (!game.Finished) return new HashSet<int>();

        if (game.QuickWinner.HasValue)
            return new HashSet<int> { game.QuickWinner.Value };

        if (game.Rounds.Count == 0) return new HashSet<int>();

        if (game.Rules.TeamMode && game.Players.Count == 4)
        {
            var teamT = ScoringCalculator.GetTeamTotals(game);
            var winTeam = teamT[0] >= teamT[1] ? 0 : 1;
            return new HashSet<int> { winTeam * 2, winTeam * 2 + 1 };
        }

        var totals = ScoringCalculator.GetPlayerTotals(game);
        var max    = totals.Max();
        var result = new HashSet<int>();
        for (int i = 0; i < totals.Length; i++)
            if (totals[i] == max) result.Add(i);
        return result;
    }

    private sealed class Builder(string name)
    {
        public string  Name           { get; } = name;
        public int     GamesPlayed;
        public int     GamesWon;
        public int     RoundsAsBidder;
        public int     BidderWins;
        public int     BidderAbgegangen;
        public int     BidderLosses;
        public int     TotalBidSum;
        public int     BidCountNotAbg;
        public int     DurchPlayed;
        public int     DurchWon;
        public int     BettelPlayed;
        public int     BettelWon;
        public decimal MoneyBalance;

        public PlayerStats Build() => new(
            Name, GamesPlayed, GamesWon, RoundsAsBidder,
            BidderWins, BidderAbgegangen, BidderLosses,
            TotalBidSum, BidCountNotAbg,
            DurchPlayed, DurchWon, BettelPlayed, BettelWon,
            MoneyBalance);
    }
}

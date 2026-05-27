using System.Globalization;
using BinokelTracker.Models;

namespace BinokelTracker.Services;

public record GlobalStats(
    int TotalGames,
    int FinishedGames,
    int TotalRounds,
    IReadOnlyList<PlayerStats> Players,
    RoundStats RoundStats,
    IReadOnlyList<HeadToHeadStats> HeadToHead);

public record RoundStats(
    IReadOnlyDictionary<TrumpSuit, int> TrumpFrequency,
    double AvgTotalPointsPerRound,
    IReadOnlyDictionary<string, int> RoundWinnerCount);

public record HeadToHeadStats(
    string PlayerA,
    string PlayerB,
    int GamesShared,
    int AWins,
    int BWins,
    int Ties,
    double AvgPointDiff);

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
    decimal MoneyBalance,
    int HighestGameScore,
    int LowestGameScore,
    double AvgFinalScore,
    int LongestWinStreak,
    int HighestBidEver,
    int NormalRoundsPlayed,
    int TotalMeldPoints,
    int TotalTrickPoints,
    int LastTrickWins,
    int HighestMeldInRound,
    IReadOnlyDictionary<MeldType, int> MeldTypeFrequency)
{
    public double BidderWinRate    => RoundsAsBidder    > 0 ? (double)BidderWins       / RoundsAsBidder    * 100 : 0;
    public double AbgegangenRate   => RoundsAsBidder    > 0 ? (double)BidderAbgegangen / RoundsAsBidder    * 100 : 0;
    public double AvgBidValue      => BidCountNotAbg    > 0 ? (double)TotalBidSum      / BidCountNotAbg          : 0;
    public double AvgMeldPoints    => NormalRoundsPlayed > 0 ? (double)TotalMeldPoints  / NormalRoundsPlayed      : 0;
    public double AvgTrickPoints   => NormalRoundsPlayed > 0 ? (double)TotalTrickPoints / NormalRoundsPlayed      : 0;
    public double MeldToTrickRatio => AvgTrickPoints    > 0 ? AvgMeldPoints / AvgTrickPoints                     : 0;
    public double LastTrickWinRate => NormalRoundsPlayed > 0 ? (double)LastTrickWins / NormalRoundsPlayed * 100   : 0;
}

public static class StatisticsService
{
    public static GlobalStats Compute(IReadOnlyList<Game> games)
    {
        var builders = new Dictionary<string, Builder>(StringComparer.Ordinal);

        Builder Get(PlayerRef player)
        {
            var key = player.UserId ?? player.DisplayName;
            if (!builders.TryGetValue(key, out var b))
                builders[key] = b = new Builder(player.DisplayName);
            else if (b.Name != player.DisplayName)
                b.Name = player.DisplayName;
            return b;
        }

        int totalRounds = 0;

        var trumpFreq          = new Dictionary<TrumpSuit, int>();
        var roundWinnerCount   = new Dictionary<string, int>(StringComparer.Ordinal);
        int  normalRoundCount  = 0;
        long normalPointsTotal = 0;

        var h2hMap = new Dictionary<(string, string), H2HBuilder>();

        foreach (var game in games)
        {
            totalRounds += game.Rounds.Count;

            var winnerIndices = GetWinnerIndices(game);

            for (int pi = 0; pi < game.Players.Count; pi++)
            {
                var b = Get(game.Players[pi]);
                b.GamesPlayed++;
                if (winnerIndices.Contains(pi)) b.GamesWon++;
            }

            AccumulateMoney(game, winnerIndices, builders);

            if (game.Finished)
            {
                var totals = game.GetPlayerTotals();
                for (int pi = 0; pi < game.Players.Count; pi++)
                {
                    var b     = Get(game.Players[pi]);
                    int score = pi < totals.Length ? totals[pi] : 0;
                    b.FinishedGameScores.Add(score);
                    b.GameResults.Add((game.Date, winnerIndices.Contains(pi)));
                    if (game.Rounds.Count > 0)
                        b.FinishedGameScoresWithRounds.Add(score);
                }
                AccumulateH2H(game, totals, h2hMap);
            }

            foreach (var round in game.Rounds)
            {
                if (round.Bidder >= game.Players.Count) continue;
                var bidder = Get(game.Players[round.Bidder]);

                if (round.Type == RoundType.Durch)
                {
                    bidder.DurchPlayed++;
                    if (round.Won) bidder.DurchWon++;
                }
                else if (round.Type == RoundType.Bettel)
                {
                    bidder.BettelPlayed++;
                    if (round.Won) bidder.BettelWon++;
                }
                else
                {
                    bidder.RoundsAsBidder++;
                    bidder.TotalBidSum   += round.Bid;
                    bidder.BidCountNotAbg++;
                    if (round.Bid > bidder.HighestBidEver)
                        bidder.HighestBidEver = round.Bid;
                    if (round.Abgegangen) bidder.BidderAbgegangen++;
                    else if (round.Won)   bidder.BidderWins++;
                    else                  bidder.BidderLosses++;

                    for (int pi = 0; pi < round.PlayerScores.Count && pi < game.Players.Count; pi++)
                    {
                        var ps = round.PlayerScores[pi];
                        var pb = Get(game.Players[pi]);
                        pb.NormalRoundsPlayed++;
                        pb.TotalMeldPoints  += ps.Meld;
                        pb.TotalTrickPoints += ps.Tricks;
                        if (ps.Meld > pb.HighestMeldInRound)
                            pb.HighestMeldInRound = ps.Meld;
                        if (round.LastTrickWinner == pi)
                            pb.LastTrickWins++;
                        if (ps.MeldTypes != null)
                            foreach (var mt in ps.MeldTypes)
                            {
                                pb.MeldTypeFrequency.TryGetValue(mt, out var c);
                                pb.MeldTypeFrequency[mt] = c + 1;
                            }
                    }

                    if (round.Trumpf.HasValue)
                    {
                        trumpFreq.TryGetValue(round.Trumpf.Value, out var tc);
                        trumpFreq[round.Trumpf.Value] = tc + 1;
                    }

                    normalRoundCount++;
                    normalPointsTotal += round.PlayerScores.Sum(ps => ps.Meld + ps.Tricks);

                    var scores = round.CalcScores(game.Rules);
                    for (int pi = 0; pi < scores.Length && pi < game.Players.Count; pi++)
                    {
                        if (pi == round.Bidder) continue;
                        if (scores[pi] > 0)
                        {
                            var name = game.Players[pi].DisplayName;
                            roundWinnerCount.TryGetValue(name, out var wc);
                            roundWinnerCount[name] = wc + 1;
                        }
                    }
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

        var roundStats = new RoundStats(
            TrumpFrequency:        trumpFreq,
            AvgTotalPointsPerRound: normalRoundCount > 0 ? (double)normalPointsTotal / normalRoundCount : 0,
            RoundWinnerCount:      roundWinnerCount);

        var headToHead = h2hMap.Values
            .Select(h => h.Build())
            .OrderByDescending(h => h.GamesShared)
            .ThenBy(h => h.PlayerA)
            .ToList();

        return new GlobalStats(
            TotalGames:    games.Count,
            FinishedGames: games.Count(g => g.Finished),
            TotalRounds:   totalRounds,
            Players:       players,
            RoundStats:    roundStats,
            HeadToHead:    headToHead);
    }

    public static decimal GameNetGain(Game game, int playerIndex)
    {
        if (!game.Finished) return 0;
        if (!decimal.TryParse(game.Einsatz, NumberStyles.Any, CultureInfo.InvariantCulture, out var einsatz) || einsatz == 0)
            return 0;

        var winners = GetWinnerIndices(game);
        if (winners.Count == 0) return 0;

        int losers = game.Players.Count - winners.Count;
        return winners.Contains(playerIndex)
            ? einsatz * losers / winners.Count
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
            var key = game.Players[i].UserId ?? game.Players[i].DisplayName;
            if (!builders.TryGetValue(key, out var b)) continue;
            b.MoneyBalance += winnerIndices.Contains(i)
                ? einsatz * losers / winnerIndices.Count
                : -einsatz;
        }
    }

    private static void AccumulateH2H(Game game, int[] totals, Dictionary<(string, string), H2HBuilder> map)
    {
        var names = game.Players.Select(p => p.DisplayName).ToList();
        for (int a = 0; a < names.Count; a++)
        {
            for (int b = a + 1; b < names.Count; b++)
            {
                bool aFirst = string.Compare(names[a], names[b], StringComparison.Ordinal) <= 0;
                var  key    = aFirst ? (names[a], names[b]) : (names[b], names[a]);

                if (!map.TryGetValue(key, out var hb))
                    map[key] = hb = new H2HBuilder(key.Item1, key.Item2);

                hb.GamesShared++;

                int aScore = a < totals.Length ? totals[a] : 0;
                int bScore = b < totals.Length ? totals[b] : 0;
                hb.TotalDiff += aFirst ? aScore - bScore : bScore - aScore;

                if      (aScore > bScore) { if (aFirst) hb.AWins++; else hb.BWins++; }
                else if (bScore > aScore) { if (aFirst) hb.BWins++; else hb.AWins++; }
                else                        hb.Ties++;
            }
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
            var teamT   = ScoringCalculator.GetTeamTotals(game);
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

    private static int ComputeStreak(IEnumerable<bool> results)
    {
        int max = 0, cur = 0;
        foreach (var won in results)
        {
            if (won) { cur++; if (cur > max) max = cur; }
            else cur = 0;
        }
        return max;
    }

    private sealed class H2HBuilder(string a, string b)
    {
        public string A = a, B = b;
        public int  GamesShared, AWins, BWins, Ties;
        public long TotalDiff;

        public HeadToHeadStats Build() => new(
            A, B, GamesShared, AWins, BWins, Ties,
            GamesShared > 0 ? (double)TotalDiff / GamesShared : 0);
    }

    private sealed class Builder(string name)
    {
        public string  Name              { get; set; } = name;
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
        public int     HighestBidEver;
        public int     NormalRoundsPlayed;
        public int     TotalMeldPoints;
        public int     TotalTrickPoints;
        public int     LastTrickWins;
        public int     HighestMeldInRound;
        public Dictionary<MeldType, int>    MeldTypeFrequency        = new();
        public List<int>                    FinishedGameScores       = new();
        public List<int>                    FinishedGameScoresWithRounds = new();
        public List<(long Date, bool Won)>  GameResults              = new();

        public PlayerStats Build()
        {
            int    highest  = FinishedGameScores.Count            > 0 ? FinishedGameScores.Max()            : 0;
            int    lowest   = FinishedGameScoresWithRounds.Count  > 0 ? FinishedGameScoresWithRounds.Min()  : 0;
            double avgScore = FinishedGameScores.Count            > 0 ? FinishedGameScores.Average()        : 0;
            int    streak   = ComputeStreak(GameResults.OrderBy(r => r.Date).Select(r => r.Won));

            return new PlayerStats(
                Name, GamesPlayed, GamesWon, RoundsAsBidder,
                BidderWins, BidderAbgegangen, BidderLosses,
                TotalBidSum, BidCountNotAbg,
                DurchPlayed, DurchWon, BettelPlayed, BettelWon,
                MoneyBalance,
                HighestGameScore:    highest,
                LowestGameScore:     lowest,
                AvgFinalScore:       avgScore,
                LongestWinStreak:    streak,
                HighestBidEver:      HighestBidEver,
                NormalRoundsPlayed:  NormalRoundsPlayed,
                TotalMeldPoints:     TotalMeldPoints,
                TotalTrickPoints:    TotalTrickPoints,
                LastTrickWins:       LastTrickWins,
                HighestMeldInRound:  HighestMeldInRound,
                MeldTypeFrequency:   MeldTypeFrequency);
        }
    }
}

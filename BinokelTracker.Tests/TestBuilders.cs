using BinokelTracker.Models;

namespace BinokelTracker.Tests;

/// <summary>
/// Hilfsmethoden zum schnellen Erstellen von Testdaten.
/// Ziel: Tests leserlich halten ohne Boilerplate.
/// </summary>
internal static class Build
{
    // ── Spiele ─────────────────────────────────────────────────────────────

    internal static Game Game(
        string[] players,
        RuleSet?  rules    = null,
        bool      finished = false,
        string    einsatz  = "0") => new()
    {
        Id       = 1,
        Date     = 0,
        Players  = players.ToList(),
        Rules    = rules ?? Rules.Default(),
        Finished = finished,
        Einsatz  = einsatz,
    };

    // ── Runden ─────────────────────────────────────────────────────────────

    /// Normale Runde. Won wird automatisch aus Meld+Stiche vs. Reizwert berechnet.
    internal static Round NormalRound(
        int   bidder,
        int   bid,
        int[] meld,
        int[] tricks,
        bool[]? abgegangen = null)
    {
        var abg        = abgegangen ?? new bool[meld.Length];
        bool bidderAbg = abg[bidder];
        bool won       = !bidderAbg && (meld[bidder] + tricks[bidder]) >= bid;

        return new Round
        {
            Id     = 1,
            Type   = RoundType.Normal,
            Bidder = bidder,
            Bid    = bid,
            Won    = won,
            PlayerScores = meld.Select((m, i) => new PlayerScore
            {
                Meld       = m,
                Tricks     = tricks[i],
                Abgegangen = abg[i],
            }).ToList()
        };
    }

    internal static Round DurchRound(int bidder, bool won, int playerCount = 3) => new()
    {
        Type   = RoundType.Durch,
        Bidder = bidder,
        Won    = won,
        PlayerScores = Enumerable.Range(0, playerCount)
            .Select(_ => new PlayerScore()).ToList()
    };

    internal static Round BettelRound(int bidder, bool won, int playerCount = 3) => new()
    {
        Type   = RoundType.Bettel,
        Bidder = bidder,
        Won    = won,
        PlayerScores = Enumerable.Range(0, playerCount)
            .Select(_ => new PlayerScore()).ToList()
    };

    // ── Regelsets ──────────────────────────────────────────────────────────

    internal static class Rules
    {
        internal static RuleSet Default() => new()
        {
            Players           = 3,
            TargetScore       = 1000,
            DoubleMinus       = false,
            AllowDurch        = false,
            AllowBettel       = false,
            AllowAbgehen      = true,
            BidderOnlyAbgehen = true,
            DurchPoints       = 1000,
            BettelPoints      = 1000,
            DurchSeparate     = true,
        };

        internal static RuleSet DoubleMinus()      => Default().With(r => r.DoubleMinus       = true);
        internal static RuleSet WithDurch()        => Default().With(r => r.AllowDurch         = true);
        internal static RuleSet WithBettel()       => Default().With(r => r.AllowBettel        = true);
        internal static RuleSet AllCanAbgehen()    => Default().With(r => r.BidderOnlyAbgehen  = false);
        internal static RuleSet DurchNotSeparate() => WithDurch().With(r => r.DurchSeparate    = false);
        internal static RuleSet TeamMode()         => Default().With(r =>
        {
            r.Players     = 4;
            r.TeamMode    = true;
            r.TargetScore = 1500;
        });
    }
}

/// <summary>
/// Klon-Helfer für RuleSet (Klasse, kein Record — kein `with`-Ausdruck möglich).
/// </summary>
internal static class RuleSetExtensions
{
    internal static RuleSet With(this RuleSet src, Action<RuleSet> configure)
    {
        var copy = src.Clone();
        configure(copy);
        return copy;
    }
}

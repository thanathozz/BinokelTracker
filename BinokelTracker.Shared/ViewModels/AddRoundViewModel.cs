using BinokelTracker.Models;
using BinokelTracker.Services;

namespace BinokelTracker.ViewModels;

public enum InputMode { Overview, StepByStep }

public class AddRoundViewModel
{
    private readonly Game _game;

    public AddRoundViewModel(Game game)
    {
        _game = game;
        ResetInputs();
    }

    // ── State ──────────────────────────────────────────────────────────────
    public InputMode Mode { get; private set; } = InputMode.StepByStep;
    public RoundType Type { get; private set; } = RoundType.Normal;
    public int Bidder { get; private set; }
    public string Bid { get; set; } = "";
    public bool Won { get; set; } = true;
    public List<bool> Abgegangen { get; private set; } = new();
    public List<string> Meld { get; private set; } = new();
    public List<string> Tricks { get; private set; } = new();
    public int Step { get; private set; }
    public bool Forward { get; private set; } = true;

    // ── Computed ───────────────────────────────────────────────────────────
    public bool IsSpecial => Type is RoundType.Durch or RoundType.Bettel;
    public int BidValue => int.TryParse(Bid, out var v) ? v : 0;
    public string MinusPreview => _game.Rules.DoubleMinus ? $"−{BidValue * 2} (doppelt)" : $"−{BidValue}";
    public bool BidderAbgegangen => Abgegangen.Count > Bidder && Abgegangen[Bidder];
    public bool AbgehenAllowed => _game.Rules.AllowAbgehen;

    private int BidderTotal =>
        (int.TryParse(Meld.ElementAtOrDefault(Bidder) ?? "", out var m) ? m : 0) +
        (int.TryParse(Tricks.ElementAtOrDefault(Bidder) ?? "", out var t) ? t : 0);

    public bool BidderWon => !BidderAbgegangen && BidValue > 0 && BidderTotal >= BidValue;

    public bool CanSaveOverview => true;

    public int TotalSteps => IsSpecial ? 2 : 5;

    public string[] StepLabels => IsSpecial
        ? new[] { "Spieler", "Ergebnis" }
        : new[] { "Reizer", "Reizwert", "Gemeldet", "Stiche", "Ergebnis" };

    public bool CanAdvance => Step switch
    {
        1 when !IsSpecial => BidderAbgegangen || BidValue > 0,
        _ => true
    };

    // ── Commands ───────────────────────────────────────────────────────────
    public void SetBidder(int idx) => Bidder = idx;

    public void SetMode(InputMode mode)
    {
        Mode = mode;
        Step = 0;
        Forward = true;
    }

    public void SetType(RoundType t)
    {
        Type = t;
        Step = 0;
        Won = true;
        Abgegangen = _game.Players.Select(_ => false).ToList();
    }

    public void GoNext()
    {
        if (!CanAdvance || Step >= TotalSteps - 1) return;
        Forward = true;
        Step++;
        if (Step == 3 && BidderAbgegangen) Step++; // Stiche-Schritt überspringen
    }

    public void GoPrev()
    {
        if (Step == 0) return;
        Forward = false;
        Step--;
        if (Step == 3 && BidderAbgegangen) Step--; // Stiche-Schritt rückwärts überspringen
    }

    // ── Output ─────────────────────────────────────────────────────────────
    public ScoreBreakdown[] GetScorePreviews()
    {
        var meld   = Meld.Select(m => int.TryParse(m, out var v) ? v : 0).ToArray();
        var tricks = Tricks.Select(t => int.TryParse(t, out var v) ? v : 0).ToArray();
        return ScoringCalculator.CalcNormalPreview(
            Bidder, BidValue, Abgegangen.ToArray(), meld, tricks, _game.Rules);
    }

    public Round BuildRound() => new Round
    {
        Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Type = Type,
        Bidder = Bidder,
        Bid = BidValue,
        Won = IsSpecial ? Won : BidderWon,
        PlayerScores = _game.Players.Select((_, i) => new PlayerScore
        {
            Meld = (i == Bidder && BidderAbgegangen) ? 0 : (int.TryParse(Meld[i], out var m) ? m : 0),
            Tricks = BidderAbgegangen ? 0 : (int.TryParse(Tricks[i], out var t) ? t : 0),
            Abgegangen = i == Bidder && BidderAbgegangen,
        }).ToList()
    };

    // ── Init ───────────────────────────────────────────────────────────────
    private void ResetInputs()
    {
        Step = 0;
        Forward = true;
        Abgegangen = _game.Players.Select(_ => false).ToList();
        Meld = _game.Players.Select(_ => "").ToList();
        Tricks = _game.Players.Select(_ => "").ToList();
    }
}

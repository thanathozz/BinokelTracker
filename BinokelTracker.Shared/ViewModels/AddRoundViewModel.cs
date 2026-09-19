using BinokelTracker.Models;
using BinokelTracker.Services;

namespace BinokelTracker.ViewModels;

public enum InputMode { Overview, StepByStep }

/// <summary>
/// Alle möglichen Formularschritte im Schritt-für-Schritt-Modus.
/// Die aktive Reihenfolge wird in ActiveSteps festgelegt — dort eine Zeile ändern genügt.
/// </summary>
public enum FormStep { Spielart, Reizwert, Melden, Stiche, LetzterStich, Ergebnis }

/// <summary>
/// Hält den gesamten Zustand des Rundenformulars und berechnet,
/// was wann angezeigt wird. Das Razor-Template enthält keine Spiellogik.
/// </summary>
public class AddRoundViewModel
{
    private readonly Game _game;
    private long? _editingId;

    public AddRoundViewModel(Game game)
    {
        _game = game;
        ResetInputs();
    }

    // ══════════════════════════════════════════════════════════════════════
    // State — Eingaben des Benutzers
    // ══════════════════════════════════════════════════════════════════════

    public InputMode Mode    { get; private set; } = InputMode.StepByStep;
    public RoundType Type    { get; private set; } = RoundType.Normal;
    public int       Bidder  { get; private set; }
    public string    Bid     { get; set; } = "";
    public bool      Won     { get; set; } = true;

    public bool OverviewShowPreview { get; private set; }

    public List<bool>   Abgegangen      { get; private set; } = new();
    public List<string> Meld            { get; private set; } = new();
    public List<string> Tricks          { get; private set; } = new();
    public int          LastTrickWinner { get; private set; }
    public TrumpSuit?   Trumpf          { get; private set; }

    // Per-player scan state
    public bool[]   Scanning      { get; private set; } = Array.Empty<bool>();
    public string?[] ScanError    { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<DetectedMeld>?[] ScanResult { get; private set; } = Array.Empty<IReadOnlyList<DetectedMeld>?>();
    public string?[] ScanRaw      { get; private set; } = Array.Empty<string?>();
    public TrumpSuit?[] ScanTrump { get; private set; } = Array.Empty<TrumpSuit?>();

    // Navigation (Schritt-für-Schritt)
    public int  Step    { get; private set; }
    public bool Forward { get; private set; } = true;

    // ══════════════════════════════════════════════════════════════════════
    // Spiellogik — abgeleitete Werte aus State + Regeln
    // ══════════════════════════════════════════════════════════════════════

    /// Durch oder Bettel (kein normales Reizspiel)
    public bool IsSpecial => Type is RoundType.Durch or RoundType.Bettel;

    /// Eingegebener Reizwert als Zahl (0 wenn leer/ungültig)
    public int BidValue => int.TryParse(Bid, out var v) ? v : 0;

    /// Hat der Reizer abgegangen?
    public bool BidderAbgegangen => Abgegangen.Count > Bidder && Abgegangen[Bidder];

    /// Abgehen-Toggle für den Reizer im Schritt "Reizwert" anzeigen?
    /// Der Reizer kann immer abgehen, außer im benutzerdefinierten Spiel mit AllowAbgehen = false.
    public bool ShowBidderAbgehenInReizwert =>
        _game.Rules.Id != "benutzerdefiniert" || _game.Rules.AllowAbgehen;


    /// Meld + Stiche des Reizers (für Gewinnprüfung)
    private int BidderTotal =>
        (int.TryParse(Meld.ElementAtOrDefault(Bidder) ?? "", out var m) ? m : 0) +
        (int.TryParse(Tricks.ElementAtOrDefault(Bidder) ?? "", out var t) ? t : 0);

    /// Maximale Stichpunkte aus den Regelwerten (8 Karten je Wert, ohne Letzter-Stich-Bonus)
    public int MaxTricksTotal =>
        8 * (_game.Rules.AssValue + _game.Rules.ZehnValue + _game.Rules.KoenigValue +
             _game.Rules.OberValue + _game.Rules.UnterValue);

    /// Summe aller eingegebenen Stichpunkte
    public int TricksSum => Tricks.Sum(t => int.TryParse(t, out var v) ? v : 0);

    /// Angezeigte Stichsumme (inkl. LastTrickBonus — wird immer vergeben)
    public int TricksSumDisplay => TricksSum + _game.Rules.LastTrickBonus;

    /// Angezeigtes Maximum (inkl. LastTrickBonus)
    public int MaxTricksTotalDisplay => MaxTricksTotal + _game.Rules.LastTrickBonus;

    /// Sind die Stichpunkte gültig (Summe == Maximum)?
    public bool TricksSumValid => TricksSum == MaxTricksTotal;

    /// Darf eine normale Runde gespeichert werden? (Stiche ok oder nicht relevant)
    public bool CanSave => IsSpecial || BidderAbgegangen || TricksSumValid;

    /// Hat der Reizer gewonnen? (Normal-Runde)
    public bool BidderWon => !BidderAbgegangen && BidValue > 0 &&
        BidderTotal + (LastTrickWinner == Bidder ? _game.Rules.LastTrickBonus : 0) >= BidValue;

    // ══════════════════════════════════════════════════════════════════════
    // Schritt-Navigation
    // ══════════════════════════════════════════════════════════════════════

    /// Aktive Schrittfolge — hier die Reihenfolge ändern, um Schritte umzusortieren.
    /// Beim Abgehen des Reizers entfällt nur "Stiche", Melden bleibt für alle anderen.
    public IReadOnlyList<FormStep> ActiveSteps => IsSpecial
        ? new[] { FormStep.Spielart, FormStep.Ergebnis }
        : BidderAbgegangen
            ? new[] { FormStep.Spielart, FormStep.Melden, FormStep.Ergebnis }
            : new[] { FormStep.Spielart, FormStep.Melden, FormStep.Stiche, FormStep.Ergebnis };

    public int      TotalSteps   => ActiveSteps.Count;
    public FormStep CurrentStep  => ActiveSteps[Step];
    public string[] StepLabels   => ActiveSteps.Select(StepLabel).ToArray();

    private static string StepLabel(FormStep s) => s switch
    {
        FormStep.Spielart => "Spieler",
        FormStep.Melden   => "Gemeldet",
        FormStep.Stiche       => "Stiche",
        FormStep.LetzterStich => "Letzter Stich",
        FormStep.Ergebnis     => "Ergebnis",
        _                 => ""
    };

    /// Darf der Benutzer zum nächsten Schritt?
    public bool CanAdvance => CurrentStep switch
    {
        FormStep.Spielart when !IsSpecial => BidValue > 0,       // Reizwert muss eingegeben sein
        FormStep.Stiche                   => TricksSumValid,
        _                                 => true
    };

    // ══════════════════════════════════════════════════════════════════════
    // Darstellungshelfer — was zeige ich wann an?
    // ══════════════════════════════════════════════════════════════════════

    /// Spielart-Auswahl (Normal/Durch/Bettel) anzeigen?
    public bool ShowGameTypeSelector => _game.Rules.AllowDurch || _game.Rules.AllowBettel;

    /// Label über der Spieler-Auswahl
    public string BidderLabel => IsSpecial ? "Spieler" : "Reizer";

    /// Frage beim Durch/Bettel-Ergebnis
    public string SpecialResultLabel => Type == RoundType.Durch
        ? "Alle Stiche gemacht?"
        : "Keinen Stich gemacht?";

    /// Formatierte Durch/Bettel-Punktzahl mit Vorzeichen
    public string DurchPointsDisplay => Won
        ? $"+{_game.Rules.DurchPoints} Punkte"
        : $"−{_game.Rules.DurchPoints} Punkte";

    /// CSS-Klasse für Gewonnen/Verloren-Färbung ("green" / "red")
    public string WonColorClass => Won ? "green" : "red";

    /// CSS-Klasse für Toggle-Zustand ("on" / "off")
    public string WonClass => Won ? "on" : "off";

    /// Minus-Vorschau für nicht bestandenes Reizspiel
    public string MinusPreview => _game.Rules.DoubleMinus
        ? $"−{BidValue * 2} (doppelt)"
        : $"−{BidValue}";

    /// Abgehen-Toggle-Label für den Reizer in Schritt 1
    public string BidderAbgehenLabel => BidderAbgegangen
        ? "Ja — abgegangen"
        : "Nein — spielt weiter";

    /// Soll der Abgehen-Toggle für diesen Spieler angezeigt werden? (Übersicht-Modus)
    public bool ShowAbgehenFor(int idx)
    {
        if (idx == Bidder)
            return _game.Rules.Id != "benutzerdefiniert" || _game.Rules.AllowAbgehen;
        return _game.Rules.AllowAbgehen && !_game.Rules.BidderOnlyAbgehen;
    }

    /// CSS-Klasse für die Spieler-Zeile (Reizer hervorgehoben)
    public string PlayerRowClass(int idx) => idx == Bidder ? "bidder-highlight" : "player-row";

    /// CSS-Klasse für den Spielernamen (Reizer in Akzentfarbe)
    public string PlayerNameClass(int idx) => idx == Bidder ? "bidder-name" : "bidder-name is-player";

    /// Ist dieser Spieler der aktuelle Reizer?
    public bool IsBidder(int idx) => idx == Bidder;

    /// <summary>
    /// Returns the display name for a player or team.
    /// In TeamMode with 4 players, indices 0 and 1 refer to teams.
    /// </summary>
    public string GetPlayerDisplayName(int idx)
    {
        if (_game.Rules.TeamMode && _game.Players.Count == 4)
        {
            // idx is team index (0 or 1)
            int p1 = idx * 2;
            int p2 = idx * 2 + 1;
            return $"{_game.Players[p1]} & {_game.Players[p2]}";
        }
        return _game.Players[idx];
    }

    /// <summary>
    /// Map a visual index (player or team) to the actual data index in the lists.
    /// </summary>
    public int MapToActualIdx(int visualIdx)
    {
        if (_game.Rules.TeamMode && _game.Players.Count == 4)
        {
            return visualIdx * 2;
        }
        return visualIdx;
    }

    /// CSS-Klasse für Abgehen-Toggle eines Spielers ("on" / "off")
    public string AbgehenClass(int idx) => Abgegangen[idx] ? "on" : "off";

    // ══════════════════════════════════════════════════════════════════════
    // Aktionen — Benutzereingaben
    // ══════════════════════════════════════════════════════════════════════

    public void ShowOverviewPreview() => OverviewShowPreview = true;
    public void HideOverviewPreview() => OverviewShowPreview = false;

    public void SetTrump(TrumpSuit? t) => Trumpf = t;

    public async Task ScanMeldAsync(int playerIdx, IMeldScanService scanner)
    {
        Scanning[playerIdx] = true;
        ScanError[playerIdx] = null;

        var result = await scanner.ScanHandAsync();

        Scanning[playerIdx] = false;
        ScanRaw[playerIdx]  = result.RawResponse;
        if (!result.Success)
        {
            ScanError[playerIdx] = result.Error;
            return;
        }

        ScanResult[playerIdx] = result.Combinations;
        ScanTrump[playerIdx]  = result.DetectedTrump;
        Meld[playerIdx] = result.TotalPoints.ToString();
        if (Trumpf is null && result.DetectedTrump is not null)
            Trumpf = result.DetectedTrump;
    }

    public void SetBidder(int idx) => Bidder = idx;

    public void SetLastTrickWinner(int idx) => LastTrickWinner = idx;

    // Tracks which slot was last auto-filled so it can be recalculated as the user keeps typing.
    private int _autoFilledIdx = -1;

    public void SetTrick(int idx, string value)
    {
        // If TeamMode is active, map team index (0,1) to player index (0,2)
        int actualIdx = _game.Rules.TeamMode && _game.Players.Count == 4 ? idx * 2 : idx;
        
        Tricks[actualIdx] = value;
        if (_game.Rules.TeamMode && _game.Players.Count == 4)
        {
            // Team Logic: 2 teams, total must be MaxTricksTotal
            if (!int.TryParse(value, out int cur)) return;

            int otherTeamIdx = (idx == 0) ? 2 : 0;
            int auto = MaxTricksTotal - cur;
            
            if (auto >= 0) { Tricks[otherTeamIdx] = auto.ToString(); }
            else             Tricks[otherTeamIdx] = "";
        }
        else if (Tricks.Count == 3)
        {
            // Original 3-player logic
            if (!int.TryParse(value, out int curVal)) return; 

            if (idx == _autoFilledIdx) { _autoFilledIdx = -1; return; }

            var others = Enumerable.Range(0, 3)
                .Where(i => i != idx)
                .Select(i => (ok: int.TryParse(Tricks[i], out var v), v, i))
                .ToList();
            var empty = others.Where(x => !x.ok).ToList();

            if (empty.Count == 1)
            {
                int auto = MaxTricksTotal - curVal - others.First(x => x.ok).v;
                if (auto >= 0) { _autoFilledIdx = empty[0].i; Tricks[_autoFilledIdx] = auto.ToString(); }
                else             Tricks[empty[0].i] = "";
            }
            else if (empty.Count == 0 && _autoFilledIdx >= 0)
            {
                int auto = MaxTricksTotal - curVal - others.First(x => x.i != _autoFilledIdx).v;
                Tricks[_autoFilledIdx] = auto >= 0 ? auto.ToString() : "";
            }
        }
    }

    public void ToggleAbgegangen(int idx)
    {
        Abgegangen[idx] = !Abgegangen[idx];
        // Bidder just folded → clear all tricks so TricksSum == 0 and trueAbgang is detected correctly
        if (idx == Bidder && Abgegangen[idx])
        {
            for (int i = 0; i < Tricks.Count; i++) Tricks[i] = "";
            _autoFilledIdx = -1;
        }
    }

    public void ToggleWon() => Won = !Won;

    public void SetMode(InputMode mode)
    {
        Mode                = mode;
        Step                = 0;
        Forward             = true;
        OverviewShowPreview = false;
    }

    public void SetType(RoundType t)
    {
        Type       = t;
        Step       = 0;
        Won        = true;
        Abgegangen = _game.Players.Select(_ => false).ToList();
    }

    public void GoNext()
    {
        if (!CanAdvance || Step >= TotalSteps - 1) return;
        Forward = true;
        Step++;
    }

    public void GoPrev()
    {
        if (Step == 0) return;
        Forward = false;
        Step--;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Ausgabe — Runde bauen + Punktevorschau
    // ══════════════════════════════════════════════════════════════════════

    /// Detaillierte Punktevorschau für jeden Spieler (Schritt "Ergebnis")
    public ScoreBreakdown[] GetScorePreviews()
    {
        var meld      = Meld.Select(m => int.TryParse(m, out var v) ? v : 0).ToArray();
        var tricks    = Tricks.Select(t => int.TryParse(t, out var v) ? v : 0).ToArray();
        bool trueAbgang = BidderAbgegangen && TricksSum == 0;
        var abgArray  = Abgegangen.Select((a, i) => a && (i != Bidder || trueAbgang)).ToArray();
        return ScoringCalculator.CalcNormalPreview(
            Bidder, BidValue, abgArray, meld, tricks, _game.Rules,
            trueAbgang ? -1 : LastTrickWinner);
    }

    /// Baut das fertige Round-Objekt zum Speichern
    public Round BuildRound()
    {
        bool trueAbgang = BidderAbgegangen && TricksSum == 0;
        return new Round
        {
            Id              = _editingId ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Type            = Type,
            Bidder          = Bidder,
            Bid             = BidValue,
            Won             = IsSpecial ? Won : BidderWon,
            LastTrickWinner = (IsSpecial || trueAbgang) ? -1 : LastTrickWinner,
            Trumpf          = IsSpecial ? null : Trumpf,
            PlayerScores    = _game.Players.Select((_, i) => new PlayerScore
            {
                Meld       = (i == Bidder && trueAbgang) ? 0 : (int.TryParse(Meld[i],   out var m) ? m : 0),
                Tricks     = trueAbgang ? 0 : (int.TryParse(Tricks[i], out var t) ? t : 0),
                Abgegangen = i == Bidder && trueAbgang,
            }).ToList()
        };
    }

    // ══════════════════════════════════════════════════════════════════════
    // Init
    // ══════════════════════════════════════════════════════════════════════

    public void InitFromRound(Round round)
    {
        _editingId      = round.Id;
        Type            = round.Type;
        Bidder          = round.Bidder;
        Bid             = round.Bid > 0 ? round.Bid.ToString() : "";
        Won             = round.Won;
        Trumpf          = round.Trumpf;
        LastTrickWinner = round.LastTrickWinner >= 0 ? round.LastTrickWinner : 0;

        for (int i = 0; i < _game.Players.Count; i++)
        {
            var ps      = i < round.PlayerScores.Count ? round.PlayerScores[i] : new PlayerScore();
            Meld[i]       = ps.Meld   > 0 ? ps.Meld.ToString()   : "";
            Tricks[i]     = ps.Tricks > 0 ? ps.Tricks.ToString() : "";
            Abgegangen[i] = ps.Abgegangen;
        }
    }

    private void ResetInputs()
    {
        Step           = 0;
        Forward        = true;
        Trumpf         = null;
        _autoFilledIdx = -1;
        Abgegangen     = _game.Players.Select(_ => false).ToList();
        Meld           = _game.Players.Select(_ => "").ToList();
        Tricks         = _game.Players.Select(_ => "").ToList();
        Scanning   = new bool[_game.Players.Count];
        ScanError  = new string?[_game.Players.Count];
        ScanResult = new IReadOnlyList<DetectedMeld>?[_game.Players.Count];
        ScanRaw    = new string?[_game.Players.Count];
        ScanTrump  = new TrumpSuit?[_game.Players.Count];
    }
}

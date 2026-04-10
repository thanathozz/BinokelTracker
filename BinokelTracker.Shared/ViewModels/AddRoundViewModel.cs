using BinokelTracker.Models;
using BinokelTracker.Services;

namespace BinokelTracker.ViewModels;

public enum InputMode { Overview, StepByStep }

/// <summary>
/// Alle möglichen Formularschritte im Schritt-für-Schritt-Modus.
/// Die aktive Reihenfolge wird in ActiveSteps festgelegt — dort eine Zeile ändern genügt.
/// </summary>
public enum FormStep { Spielart, Reizwert, Melden, Stiche, Ergebnis }

/// <summary>
/// Hält den gesamten Zustand des Rundenformulars und berechnet,
/// was wann angezeigt wird. Das Razor-Template enthält keine Spiellogik.
/// </summary>
public class AddRoundViewModel
{
    private readonly Game _game;

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

    public List<bool>   Abgegangen { get; private set; } = new();
    public List<string> Meld       { get; private set; } = new();
    public List<string> Tricks     { get; private set; } = new();

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

    /// Hat der Reizer gewonnen? (Normal-Runde)
    public bool BidderWon => !BidderAbgegangen && BidValue > 0 && BidderTotal >= BidValue;

    // ══════════════════════════════════════════════════════════════════════
    // Schritt-Navigation
    // ══════════════════════════════════════════════════════════════════════

    /// Aktive Schrittfolge — hier die Reihenfolge ändern, um Schritte umzusortieren.
    /// Beim Abgehen des Reizers entfällt nur "Stiche", Melden bleibt für alle anderen.
    public IReadOnlyList<FormStep> ActiveSteps => IsSpecial
        ? new[] { FormStep.Spielart,  FormStep.Ergebnis }
        : BidderAbgegangen
            ? new[] { FormStep.Spielart, FormStep.Reizwert, FormStep.Melden, FormStep.Ergebnis }
            : new[] { FormStep.Spielart, FormStep.Reizwert, FormStep.Melden, FormStep.Stiche, FormStep.Ergebnis };

    public int      TotalSteps   => ActiveSteps.Count;
    public FormStep CurrentStep  => ActiveSteps[Step];
    public string[] StepLabels   => ActiveSteps.Select(StepLabel).ToArray();

    private static string StepLabel(FormStep s) => s switch
    {
        FormStep.Spielart => "Spieler",
        FormStep.Reizwert => "Reizwert",
        FormStep.Melden   => "Gemeldet",
        FormStep.Stiche   => "Stiche",
        FormStep.Ergebnis => "Ergebnis",
        _                 => ""
    };

    /// Darf der Benutzer zum nächsten Schritt?
    public bool CanAdvance => CurrentStep switch
    {
        FormStep.Reizwert when !IsSpecial => BidValue > 0,  // Reizwert muss eingegeben sein
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

    /// CSS-Klasse für Abgehen-Toggle eines Spielers ("on" / "off")
    public string AbgehenClass(int idx) => Abgegangen[idx] ? "on" : "off";

    // ══════════════════════════════════════════════════════════════════════
    // Aktionen — Benutzereingaben
    // ══════════════════════════════════════════════════════════════════════

    public void SetBidder(int idx) => Bidder = idx;

    public void ToggleAbgegangen(int idx) => Abgegangen[idx] = !Abgegangen[idx];

    public void ToggleWon() => Won = !Won;

    public void SetMode(InputMode mode)
    {
        Mode    = mode;
        Step    = 0;
        Forward = true;
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
        var meld   = Meld.Select(m => int.TryParse(m, out var v) ? v : 0).ToArray();
        var tricks = Tricks.Select(t => int.TryParse(t, out var v) ? v : 0).ToArray();
        return ScoringCalculator.CalcNormalPreview(
            Bidder, BidValue, Abgegangen.ToArray(), meld, tricks, _game.Rules);
    }

    /// Baut das fertige Round-Objekt zum Speichern
    public Round BuildRound() => new Round
    {
        Id     = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Type   = Type,
        Bidder = Bidder,
        Bid    = BidValue,
        Won    = IsSpecial ? Won : BidderWon,
        PlayerScores = _game.Players.Select((_, i) => new PlayerScore
        {
            Meld       = (i == Bidder && BidderAbgegangen) ? 0 : (int.TryParse(Meld[i],   out var m) ? m : 0),
            Tricks     = BidderAbgegangen                  ? 0 : (int.TryParse(Tricks[i], out var t) ? t : 0),
            Abgegangen = i == Bidder && BidderAbgegangen,
        }).ToList()
    };

    // ══════════════════════════════════════════════════════════════════════
    // Init
    // ══════════════════════════════════════════════════════════════════════

    private void ResetInputs()
    {
        Step       = 0;
        Forward    = true;
        Abgegangen = _game.Players.Select(_ => false).ToList();
        Meld       = _game.Players.Select(_ => "").ToList();
        Tricks     = _game.Players.Select(_ => "").ToList();
    }
}

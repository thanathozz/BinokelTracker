using BinokelTracker.Models;
using BinokelTracker.Services;
using FluentAssertions;

namespace BinokelTracker.Tests;

/// <summary>
/// Testet die Kernpunkte-Berechnung: normale Runden, Durch, Bettel,
/// Abgehen, Doppelt-Minus, Teamwertung und Gesamt-Summen.
/// </summary>
public class ScoringCalculatorTests
{
    // ══════════════════════════════════════════════════════════════════════
    // Normale Runde — Reizer gewinnt / verliert
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Reizer_gewinnt_wenn_Meld_plus_Stiche_groesser_Reizwert()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 300,
            meld:   new[] { 200, 100,  50 },
            tricks: new[] { 150,  80,  40 });

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.Default());

        scores[0].Should().Be(350);  // 200 + 150
        scores[1].Should().Be(180);  // 100 + 80
        scores[2].Should().Be(90);   // 50 + 40
    }

    [Fact]
    public void Reizer_verliert_wenn_Meld_plus_Stiche_kleiner_Reizwert()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 400,
            meld:   new[] { 100, 100, 100 },
            tricks: new[] { 100, 100, 100 });  // Reizer: 200 < 400

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.Default());

        scores[0].Should().Be(-400);
    }

    [Fact]
    public void Doppelt_Minus_verdoppelt_Verlust_des_Reizers()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 300,
            meld:   new[] { 0, 0, 0 },
            tricks: new[] { 0, 0, 0 });

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.DoubleMinus());

        scores[0].Should().Be(-600);  // -300 * 2
    }

    [Fact]
    public void Reizer_trifft_genau_den_Reizwert_und_gewinnt()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 300,
            meld:   new[] { 200, 0, 0 },
            tricks: new[] { 100, 0, 0 });  // genau 300

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.Default());

        scores[0].Should().Be(300);  // kein Verlust bei Gleichstand
    }

    // ══════════════════════════════════════════════════════════════════════
    // Abgehen
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Reizer_abgegangen_zahlt_Reizwert_und_andere_erhalten_Bonus()
    {
        // 3 Spieler → Bonus = 3 × 10 = 30
        var round = Build.NormalRound(
            bidder: 0, bid: 300,
            meld:   new[] {   0, 100, 80 },
            tricks: new[] {   0,   0,  0 },
            abgegangen: new[] { true, false, false });

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.Default());

        scores[0].Should().Be(-300);        // Reizer zahlt Reizwert
        scores[1].Should().Be(100 + 30);    // Meld + Abgehen-Bonus
        scores[2].Should().Be( 80 + 30);
    }

    [Fact]
    public void Reizer_abgegangen_mit_Doppelt_Minus_zahlt_doppelten_Reizwert()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 300,
            meld:   new[] { 0, 0, 0 },
            tricks: new[] { 0, 0, 0 },
            abgegangen: new[] { true, false, false });

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.DoubleMinus());

        scores[0].Should().Be(-600);
    }

    [Fact]
    public void Team_Reizer_abgegangen_bestraft_auch_Partner()
    {
        // 4 Spieler, TeamMode: Bidder=0, Partner=2, Gegner=1,3
        // Bonus = 4 × 10 = 40
        var round = Build.NormalRound(
            bidder: 0, bid: 300,
            meld:   new[] { 0, 100, 50, 80 },
            tricks: new[] { 0,   0,  0,  0 },
            abgegangen: new[] { true, false, false, false });

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.TeamMode());

        scores[0].Should().Be(-300);        // Reizer zahlt Reizwert
        scores[2].Should().Be(-300);        // Partner zahlt ebenfalls Reizwert
        scores[1].Should().Be(100 + 40);    // Gegner: Meld + Bonus
        scores[3].Should().Be( 80 + 40);    // Gegner: Meld + Bonus
    }

    [Fact]
    public void Mitspieler_abgegangen_bekommt_null_Punkte()
    {
        // AllCanAbgehen: auch andere Spieler können abgehen
        var round = Build.NormalRound(
            bidder: 0, bid: 200,
            meld:   new[] { 200, 150, 0 },
            tricks: new[] { 100, 100, 0 },
            abgegangen: new[] { false, true, false });

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.AllCanAbgehen());

        scores[1].Should().Be(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Meldung zählt immer
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Meldung_zaehlt_auch_ohne_Stich()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 200,
            meld:   new[] { 200, 150, 0 },
            tricks: new[] { 200,   0, 0 });  // Spieler 1: kein Stich, aber Meldung zählt

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.Default());

        scores[1].Should().Be(150);  // Meldung verfällt nicht
    }

    [Fact]
    public void Ohne_Meldung_und_ohne_Stich_gibt_null_Punkte()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 200,
            meld:   new[] { 200, 0, 0 },
            tricks: new[] { 200, 0, 0 });

        var scores = ScoringCalculator.CalcRoundScores(round, Build.Rules.Default());

        scores[1].Should().Be(0);
        scores[2].Should().Be(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Durch
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Durch_gewonnen_gibt_Reizer_DurchPunkte()
    {
        var rules = Build.Rules.WithDurch();
        var round = Build.DurchRound(bidder: 1, won: true);

        var scores = ScoringCalculator.CalcRoundScores(round, rules);

        scores[1].Should().Be(rules.DurchPoints);
        scores[0].Should().Be(0);
        scores[2].Should().Be(0);
    }

    [Fact]
    public void Durch_verloren_zieht_DurchPunkte_ab()
    {
        var rules = Build.Rules.WithDurch();
        var round = Build.DurchRound(bidder: 1, won: false);

        var scores = ScoringCalculator.CalcRoundScores(round, rules);

        scores[1].Should().Be(-rules.DurchPoints);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Bettel
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Bettel_gewonnen_gibt_Reizer_BettelPunkte()
    {
        var rules = Build.Rules.WithBettel();
        var round = Build.BettelRound(bidder: 0, won: true);

        var scores = ScoringCalculator.CalcRoundScores(round, rules);

        scores[0].Should().Be(rules.BettelPoints);
    }

    [Fact]
    public void Bettel_verloren_zieht_BettelPunkte_ab()
    {
        var rules = Build.Rules.WithBettel();
        var round = Build.BettelRound(bidder: 0, won: false);

        var scores = ScoringCalculator.CalcRoundScores(round, rules);

        scores[0].Should().Be(-rules.BettelPoints);
    }

    // ══════════════════════════════════════════════════════════════════════
    // GetPlayerTotals — Rundenübergreifende Summen
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetPlayerTotals_summiert_Punkte_ueber_mehrere_Runden()
    {
        var game = Build.Game(new[] { "A", "B", "C" });
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 200, 0, 0 }, new[] { 100, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(1, 150, new[] {   0, 200, 0 }, new[] { 0, 100, 0 }));

        var totals = ScoringCalculator.GetPlayerTotals(game);

        totals[0].Should().Be(300);  // 200+100
        totals[1].Should().Be(300);  // 200+100
        totals[2].Should().Be(0);
    }

    [Fact]
    public void GetPlayerTotals_zaehlt_Durch_nicht_wenn_DurchSeparate_aktiv()
    {
        var rules = Build.Rules.WithDurch().With(r => { r.DurchSeparate = true; r.DurchPoints = 500; });
        var game  = Build.Game(new[] { "A", "B", "C" }, rules);
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.DurchRound(0, won: true));

        var totals = ScoringCalculator.GetPlayerTotals(game);

        totals[0].Should().Be(300);  // Durch ignoriert in Hauptwertung
    }

    [Fact]
    public void GetPlayerTotals_zaehlt_Durch_mit_wenn_DurchSeparate_deaktiviert()
    {
        var rules = Build.Rules.DurchNotSeparate().With(r => r.DurchPoints = 500);
        var game  = Build.Game(new[] { "A", "B", "C" }, rules);
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.DurchRound(0, won: true));

        var totals = ScoringCalculator.GetPlayerTotals(game);

        totals[0].Should().Be(800);  // 300 + 500
    }

    [Fact]
    public void GetDurchTotals_enthaelt_nur_Durch_und_Bettel_Runden()
    {
        var rules = Build.Rules.WithDurch().With(r => { r.DurchSeparate = true; r.DurchPoints = 500; });
        var game  = Build.Game(new[] { "A", "B", "C" }, rules);
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.DurchRound(0, won: true));

        var durch = ScoringCalculator.GetDurchTotals(game);

        durch[0].Should().Be(500);
        durch[1].Should().Be(0);
        durch[2].Should().Be(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Team-Modus
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetTeamTotals_addiert_Spieler_0_plus_2_fuer_Team_0()
    {
        // Team 0: Spieler 0 + 2 | Team 1: Spieler 1 + 3
        var game = Build.Game(new[] { "A", "B", "C", "D" }, Build.Rules.TeamMode());
        game.Rounds.Add(new Round
        {
            Type = RoundType.Normal, Bidder = 0, Bid = 300, Won = true,
            PlayerScores = new List<PlayerScore>
            {
                new() { Meld = 200, Tricks = 200 },  // A: 400 (gewinnt)
                new() { Meld = 100, Tricks =   0 },  // B: 100 (Meldung zählt auch ohne Stich)
                new() { Meld =   0, Tricks = 100 },  // C: 100
                new() { Meld =  50, Tricks =  50 },  // D: 100
            }
        });

        var teams = ScoringCalculator.GetTeamTotals(game);

        teams[0].Should().Be(500);   // A(400) + C(100)
        teams[1].Should().Be(200);   // B(100) + D(100)
    }
}

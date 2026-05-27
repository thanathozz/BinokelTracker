using BinokelTracker.Models;
using BinokelTracker.Services;
using BinokelTracker.ViewModels;
using FluentAssertions;

namespace BinokelTracker.Tests;

/// <summary>
/// Stellt sicher, dass der Letzter-Stich-Bonus (+10) in ALLEN relevanten
/// Berechnungsschichten korrekt in die Reizwert-Prüfung einbezogen wird.
///
/// Kernfall: Reizwert 250, Stiche 240 (alle), letzter Stich beim Reizer
/// → Gesamtpunkte 250 → soll als "gereizt" gelten.
///
/// Betroffene Schichten:
///   1. ScoringCalculator.CalcRoundScores  (für Statistik / Gesamtauswertung)
///   2. Round.CalcScores                   (für GameDetail / Spielverlauf)
///   3. ScoringCalculator.CalcNormalPreview (für Ergebnis-Vorschau im Formular)
///   4. AddRoundViewModel.BidderWon         (für Won-Flag und ViewModel-Anzeige)
/// </summary>
public class LastTrickBonusTests
{
    private static readonly RuleSet DefaultRules = Build.Rules.Default();

    // ══════════════════════════════════════════════════════════════════════
    // 1. ScoringCalculator.CalcRoundScores
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CalcRoundScores_Bidder_gewinnt_wenn_Bonus_Reizwert_genau_erreicht()
    {
        // 240 Stiche + 10 Bonus = 250 = Reizwert → Sieg
        var round = Build.NormalRound(
            bidder: 0, bid: 250,
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            lastTrickWinner: 0);

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);

        scores[0].Should().Be(250);  // 240 Stiche + 10 Bonus
    }

    [Fact]
    public void CalcRoundScores_Bidder_verliert_wenn_Bonus_Reizwert_knapp_verfehlt()
    {
        // 240 Stiche + 10 Bonus = 250 < 251 = Reizwert → Niederlage
        var round = Build.NormalRound(
            bidder: 0, bid: 251,
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            lastTrickWinner: 0);

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);

        scores[0].Should().Be(-251);
    }

    [Fact]
    public void CalcRoundScores_Bidder_verliert_ohne_letzten_Stich_bei_240_gegen_250()
    {
        // 240 Stiche, aber Mitspieler hat letzten Stich → kein Bonus → 240 < 250 → Niederlage
        var round = Build.NormalRound(
            bidder: 0, bid: 250,
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 180, 40, 20 },
            lastTrickWinner: 1);  // Mitspieler hat letzten Stich

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);

        scores[0].Should().Be(-250);
        scores[1].Should().Be(40 + 10);  // Mitspieler erhält Bonus
    }

    [Fact]
    public void CalcRoundScores_Bonus_wird_zum_Mitspieler_addiert_nicht_zum_Reizer()
    {
        // Bidder gewinnt normal, Mitspieler hat letzten Stich
        var round = Build.NormalRound(
            bidder: 0, bid: 200,
            meld:   new[] { 150, 50, 0 },
            tricks: new[] { 100, 80, 60 },
            lastTrickWinner: 2);

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);

        scores[0].Should().Be(250);       // 150+100, kein Bonus
        scores[1].Should().Be(130);       // 50+80, kein Bonus
        scores[2].Should().Be(60 + 10);   // 60 + Bonus
    }

    [Fact]
    public void CalcRoundScores_kein_Bonus_wenn_lastTrickWinner_minus_eins()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 200,
            meld:   new[] { 150, 0, 0 },
            tricks: new[] { 100, 80, 60 },
            lastTrickWinner: -1);

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);

        scores[0].Should().Be(250);  // kein Bonus
        scores[1].Should().Be(80);
        scores[2].Should().Be(60);
    }

    [Fact]
    public void CalcRoundScores_Bonus_zaehlt_auch_mit_Meldung_zum_Reizwert()
    {
        // Meldung 100 + Stiche 140 + Bonus 10 = 250 = Reizwert → Sieg
        var round = Build.NormalRound(
            bidder: 0, bid: 250,
            meld:   new[] { 100,  50, 30 },
            tricks: new[] { 140,  60, 40 },
            lastTrickWinner: 0);

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);

        scores[0].Should().Be(250);  // 100+140+10
    }

    [Fact]
    public void CalcRoundScores_DoubleMinus_korrekt_wenn_Bonus_Reizwert_nicht_erreicht()
    {
        // 240 + 10 = 250 < 251 → Verlust, DoubleMinus → -502
        var rules = Build.Rules.DoubleMinus();
        var round = Build.NormalRound(
            bidder: 0, bid: 251,
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            lastTrickWinner: 0);

        var scores = ScoringCalculator.CalcRoundScores(round, rules);

        scores[0].Should().Be(-502);
    }

    [Fact]
    public void CalcRoundScores_DoubleMinus_kein_Verlust_wenn_Bonus_Reizwert_erreicht()
    {
        // 240 + 10 = 250 = Reizwert → Sieg, kein DoubleMinus
        var rules = Build.Rules.DoubleMinus();
        var round = Build.NormalRound(
            bidder: 0, bid: 250,
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            lastTrickWinner: 0);

        var scores = ScoringCalculator.CalcRoundScores(round, rules);

        scores[0].Should().Be(250);  // Sieg, kein Minus
    }

    [Fact]
    public void CalcRoundScores_Bonus_nicht_vergeben_wenn_Bidder_abgegangen()
    {
        // Abgegangen → LastTrickWinner = -1 (wird in BuildRound so gesetzt)
        var round = Build.NormalRound(
            bidder: 0, bid: 300,
            meld:   new[] { 0, 100, 80 },
            tricks: new[] { 0,   0,  0 },
            abgegangen: new[] { true, false, false },
            lastTrickWinner: -1);

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);

        scores[0].Should().Be(-300);
        scores[1].Should().Be(100 + 30);  // Meld + Abg.-Bonus, kein Letzter-Stich-Bonus
        scores[2].Should().Be(80  + 30);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 2. Round.CalcScores (GameModels.cs) — parallele Implementierung
    //    Muss mit ScoringCalculator.CalcRoundScores identisch sein.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Round_CalcScores_Bidder_gewinnt_wenn_Bonus_Reizwert_genau_erreicht()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 250,
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            lastTrickWinner: 0);

        var scores = round.CalcScores(DefaultRules);

        scores[0].Should().Be(250);
    }

    [Fact]
    public void Round_CalcScores_Bidder_verliert_wenn_Bonus_Reizwert_knapp_verfehlt()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 251,
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            lastTrickWinner: 0);

        var scores = round.CalcScores(DefaultRules);

        scores[0].Should().Be(-251);
    }

    [Fact]
    public void Round_CalcScores_Bidder_verliert_ohne_letzten_Stich_bei_240_gegen_250()
    {
        var round = Build.NormalRound(
            bidder: 0, bid: 250,
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 180, 40, 20 },
            lastTrickWinner: 1);

        var scores = round.CalcScores(DefaultRules);

        scores[0].Should().Be(-250);
        scores[1].Should().Be(50);  // 40 + 10 Bonus
    }

    [Fact]
    public void Round_CalcScores_und_ScoringCalculator_liefern_identische_Ergebnisse()
    {
        // Beide Implementierungen müssen für dieselbe Eingabe dasselbe Ergebnis liefern
        var cases = new[]
        {
            Build.NormalRound(0, 250, new[] {   0, 0, 0 }, new[] { 240, 0, 0 }, lastTrickWinner: 0),
            Build.NormalRound(0, 251, new[] {   0, 0, 0 }, new[] { 240, 0, 0 }, lastTrickWinner: 0),
            Build.NormalRound(0, 250, new[] {   0, 0, 0 }, new[] { 180, 40, 20 }, lastTrickWinner: 1),
            Build.NormalRound(0, 200, new[] { 150, 50, 0 }, new[] { 100, 80, 60 }, lastTrickWinner: 2),
            Build.NormalRound(1, 300, new[] { 100, 200, 50 }, new[] { 50, 100, 90 }, lastTrickWinner: 1),
        };

        foreach (var round in cases)
        {
            var fromCalc  = ScoringCalculator.CalcRoundScores(round, DefaultRules);
            var fromModel = round.CalcScores(DefaultRules);

            fromModel.Should().Equal(fromCalc,
                because: $"Round.CalcScores und ScoringCalculator.CalcRoundScores müssen identisch sein (Bid={round.Bid}, LastTrick={round.LastTrickWinner})");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 3. ScoringCalculator.CalcNormalPreview (Ergebnis-Vorschau im Formular)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CalcNormalPreview_Bidder_gewinnt_wenn_Bonus_Reizwert_genau_erreicht()
    {
        var previews = ScoringCalculator.CalcNormalPreview(
            bidder: 0, bidValue: 250,
            abgegangen: new[] { false, false, false },
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            rules:  DefaultRules,
            lastTrickWinner: 0);

        previews[0].IsLoss.Should().BeFalse();
        previews[0].FinalScore.Should().Be(250);      // 240 Stiche + 10 Bonus
        previews[0].LastTrickBonus.Should().Be(10);
        previews[0].LossReason.Should().BeNull();
    }

    [Fact]
    public void CalcNormalPreview_Bidder_verliert_wenn_Bonus_Reizwert_knapp_verfehlt()
    {
        var previews = ScoringCalculator.CalcNormalPreview(
            bidder: 0, bidValue: 251,
            abgegangen: new[] { false, false, false },
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            rules:  DefaultRules,
            lastTrickWinner: 0);

        previews[0].IsLoss.Should().BeTrue();
        previews[0].FinalScore.Should().Be(-251);
        previews[0].LossReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CalcNormalPreview_Bidder_verliert_ohne_letzten_Stich_bei_240_gegen_250()
    {
        var previews = ScoringCalculator.CalcNormalPreview(
            bidder: 0, bidValue: 250,
            abgegangen: new[] { false, false, false },
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 180, 40, 20 },
            rules:  DefaultRules,
            lastTrickWinner: 1);

        previews[0].IsLoss.Should().BeTrue();
        previews[0].FinalScore.Should().Be(-250);
        previews[1].LastTrickBonus.Should().Be(10);
        previews[1].IsLoss.Should().BeFalse();
    }

    [Fact]
    public void CalcNormalPreview_Bonus_erscheint_nicht_bei_Verlust()
    {
        // Bidder verliert → letzterStichBonus darf nicht addiert werden (isLoss=true)
        var previews = ScoringCalculator.CalcNormalPreview(
            bidder: 0, bidValue: 400,
            abgegangen: new[] { false, false, false },
            meld:   new[] {   0, 0, 0 },
            tricks: new[] { 240, 0, 0 },
            rules:  DefaultRules,
            lastTrickWinner: 0);

        previews[0].IsLoss.Should().BeTrue();
        previews[0].LastTrickBonus.Should().Be(0);  // kein Bonus bei Verlust
        previews[0].FinalScore.Should().Be(-400);
    }

    [Fact]
    public void CalcNormalPreview_zeigt_korrekten_Score_ohne_letzten_Stich()
    {
        // lastTrickWinner = -1 (Standard wenn nicht explizit gesetzt)
        var previews = ScoringCalculator.CalcNormalPreview(
            bidder: 0, bidValue: 200,
            abgegangen: new[] { false, false, false },
            meld:   new[] { 150, 50, 0 },
            tricks: new[] { 100, 80, 60 },
            rules:  DefaultRules,
            lastTrickWinner: -1);

        previews[0].FinalScore.Should().Be(250);
        previews[0].LastTrickBonus.Should().Be(0);
        previews[1].LastTrickBonus.Should().Be(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 4. AddRoundViewModel.BidderWon
    // ══════════════════════════════════════════════════════════════════════

    private static AddRoundViewModel ForGame(RuleSet? rules = null)
        => new(Build.Game(new[] { "A", "B", "C" }, rules));

    [Fact]
    public void BidderWon_ist_true_wenn_Bonus_Reizwert_genau_erreicht()
    {
        var vm = ForGame();
        vm.Bid        = "250";
        vm.Tricks[0]  = "240";
        vm.SetLastTrickWinner(0);  // Bidder hat letzten Stich

        vm.BidderWon.Should().BeTrue();
    }

    [Fact]
    public void BidderWon_ist_false_wenn_Bonus_Reizwert_knapp_verfehlt()
    {
        var vm = ForGame();
        vm.Bid        = "251";
        vm.Tricks[0]  = "240";
        vm.SetLastTrickWinner(0);

        vm.BidderWon.Should().BeFalse();
    }

    [Fact]
    public void BidderWon_ist_false_wenn_kein_letzter_Stich_und_240_gegen_250()
    {
        var vm = ForGame();
        vm.Bid        = "250";
        vm.Tricks[0]  = "240";
        vm.SetLastTrickWinner(1);  // Mitspieler hat letzten Stich

        vm.BidderWon.Should().BeFalse();
    }

    [Fact]
    public void BidderWon_ist_true_ohne_Bonus_wenn_Reizwert_direkt_erreicht()
    {
        var vm = ForGame();
        vm.Bid       = "240";
        vm.Tricks[0] = "240";
        // kein expliziter lastTrickWinner gesetzt → default 0 (Bidder)
        // aber 240 >= 240 → true auch ohne Bonus
        vm.SetLastTrickWinner(1);  // Mitspieler hat letzten Stich, kein Bonus für Bidder

        vm.BidderWon.Should().BeTrue();  // 240 >= 240 ohne Bonus
    }

    [Fact]
    public void BuildRound_Won_korrekt_wenn_Bonus_entscheidend()
    {
        var vm = ForGame();
        vm.Bid        = "250";
        vm.Tricks[0]  = "240";
        vm.SetLastTrickWinner(0);

        var round = vm.BuildRound();

        round.Won.Should().BeTrue();
        round.LastTrickWinner.Should().Be(0);
    }

    [Fact]
    public void BuildRound_Won_false_wenn_Bonus_Reizwert_verfehlt()
    {
        var vm = ForGame();
        vm.Bid        = "251";
        vm.Tricks[0]  = "240";
        vm.SetLastTrickWinner(0);

        var round = vm.BuildRound();

        round.Won.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════
    // 5. Vollständiger Pfad: ViewModel → BuildRound → CalcScores
    //    Stellt sicher, dass alle Schichten konsistent zusammenarbeiten.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Vollpfad_Bid250_Tricks240_LetzterStichReizer_Score_ist_250()
    {
        // Simuliert den kompletten Spielablauf:
        // 1. Benutzer gibt Reizwert 250, Stiche 240, letzter Stich = Reizer ein
        // 2. ViewModel erkennt als Sieg (BidderWon = true)
        // 3. BuildRound erzeugt korrekte Runde
        // 4. CalcRoundScores liefert 250 Punkte für den Reizer

        var vm = ForGame();
        vm.SetBidder(0);
        vm.Bid       = "250";
        vm.Tricks[0] = "240";
        vm.Tricks[1] = "0";
        vm.Tricks[2] = "0";
        vm.SetLastTrickWinner(0);

        vm.BidderWon.Should().BeTrue("ViewModel soll Sieg erkennen");

        var round  = vm.BuildRound();
        round.Won.Should().BeTrue("BuildRound soll Won=true setzen");

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);
        scores[0].Should().Be(250, "Endpunktzahl soll 250 sein (240 Stiche + 10 Bonus)");
    }

    [Fact]
    public void Vollpfad_Bid251_Tricks240_LetzterStichReizer_Score_ist_minus_251()
    {
        var vm = ForGame();
        vm.SetBidder(0);
        vm.Bid       = "251";
        vm.Tricks[0] = "240";
        vm.Tricks[1] = "0";
        vm.Tricks[2] = "0";
        vm.SetLastTrickWinner(0);

        vm.BidderWon.Should().BeFalse("ViewModel soll Niederlage erkennen");

        var round  = vm.BuildRound();
        round.Won.Should().BeFalse();

        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);
        scores[0].Should().Be(-251, "Endpunktzahl soll -251 sein");
    }

    [Fact]
    public void Vollpfad_Bid250_Tricks240_LetzterStichMitspieler_Score_ist_minus_250()
    {
        var vm = ForGame();
        vm.SetBidder(0);
        vm.Bid       = "250";
        vm.Tricks[0] = "200";
        vm.Tricks[1] = "40";
        vm.Tricks[2] = "0";
        vm.SetLastTrickWinner(1);  // Mitspieler hat letzten Stich

        vm.BidderWon.Should().BeFalse("Reizer hat keinen Bonus, 200 < 250");

        var round  = vm.BuildRound();
        var scores = ScoringCalculator.CalcRoundScores(round, DefaultRules);

        scores[0].Should().Be(-250);
        scores[1].Should().Be(40 + 10, "Mitspieler erhält Bonus");
    }
}

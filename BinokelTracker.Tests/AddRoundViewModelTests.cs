using BinokelTracker.Models;
using BinokelTracker.Services;
using BinokelTracker.ViewModels;
using FluentAssertions;

namespace BinokelTracker.Tests;

/// <summary>
/// Testet das AddRoundViewModel: Schrittsequenz, Navigation,
/// Anzeigehelfer und die Rundenerstellung.
/// </summary>
public class AddRoundViewModelTests
{
    private static AddRoundViewModel ForGame(RuleSet? rules = null)
        => new(Build.Game(new[] { "A", "B", "C" }, rules));

    // ══════════════════════════════════════════════════════════════════════
    // Schrittsequenz (ActiveSteps)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Normale_Runde_hat_vier_Schritte()
    {
        var vm = ForGame();

        vm.ActiveSteps.Should().Equal(
            FormStep.Spielart, FormStep.Melden, FormStep.Stiche, FormStep.Ergebnis);
    }

    [Fact]
    public void Durch_hat_nur_Spielart_und_Ergebnis()
    {
        var vm = new AddRoundViewModel(Build.Game(new[] { "A", "B", "C" }, Build.Rules.WithDurch()));
        vm.SetType(RoundType.Durch);

        vm.ActiveSteps.Should().Equal(FormStep.Spielart, FormStep.Ergebnis);
        vm.TotalSteps.Should().Be(2);
    }

    [Fact]
    public void Bettel_hat_nur_Spielart_und_Ergebnis()
    {
        var vm = new AddRoundViewModel(Build.Game(new[] { "A", "B", "C" }, Build.Rules.WithBettel()));
        vm.SetType(RoundType.Bettel);

        vm.ActiveSteps.Should().Equal(FormStep.Spielart, FormStep.Ergebnis);
        vm.TotalSteps.Should().Be(2);
    }

    [Fact]
    public void Reizer_abgegangen_entfernt_Stiche_Schritt()
    {
        var vm = ForGame();
        vm.Bid = "300";
        vm.ToggleAbgegangen(0); // Reizer abgegangen

        vm.ActiveSteps.Should().Equal(FormStep.Spielart, FormStep.Melden, FormStep.Ergebnis);
        vm.TotalSteps.Should().Be(3);
    }

    [Fact]
    public void Reizer_abgegangen_zurueckgenommen_stellt_Stiche_wieder_her()
    {
        var vm = ForGame();
        vm.Bid = "300";
        vm.ToggleAbgegangen(0);  // abgegangen
        vm.ToggleAbgegangen(0);  // wieder zurück

        vm.TotalSteps.Should().Be(4);
        vm.ActiveSteps.Should().Contain(FormStep.Stiche);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Navigation (GoNext / GoPrev)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GoNext_wechselt_zum_naechsten_Schritt()
    {
        var vm = ForGame();
        vm.Bid = "200";
        vm.GoNext();

        vm.Step.Should().Be(1);
        vm.CurrentStep.Should().Be(FormStep.Melden);
    }

    [Fact]
    public void GoNext_geht_nicht_ueber_letzten_Schritt_hinaus()
    {
        var vm = ForGame();
        vm.Bid       = "200";
        vm.Tricks[0] = "100"; vm.Tricks[1] = "100"; vm.Tricks[2] = "40"; // 240 = Max ohne letzten Stich
        for (int i = 0; i < 20; i++) vm.GoNext();

        vm.Step.Should().Be(vm.TotalSteps - 1);
        vm.CurrentStep.Should().Be(FormStep.Ergebnis);
    }

    [Fact]
    public void GoPrev_wechselt_zurueck()
    {
        var vm = ForGame();
        vm.Bid = "200";
        vm.GoNext(); // → Melden (step 1)
        vm.GoNext(); // → Stiche (step 2)

        vm.GoPrev();

        vm.Step.Should().Be(1);
        vm.CurrentStep.Should().Be(FormStep.Melden);
    }

    [Fact]
    public void GoPrev_geht_nicht_vor_Schritt_null()
    {
        var vm = ForGame();
        vm.GoPrev();

        vm.Step.Should().Be(0);
    }

    [Fact]
    public void Forward_ist_true_nach_GoNext_und_false_nach_GoPrev()
    {
        var vm = ForGame();
        vm.Bid = "200";
        vm.GoNext(); // → Melden
        vm.Forward.Should().BeTrue();

        vm.GoPrev(); // → Spielart
        vm.Forward.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════
    // CanAdvance
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanAdvance_ist_false_am_Spielart_Schritt_ohne_Reizwert()
    {
        var vm = ForGame();

        vm.CurrentStep.Should().Be(FormStep.Spielart);
        vm.CanAdvance.Should().BeFalse(); // Bid noch nicht eingegeben
    }

    [Fact]
    public void CanAdvance_ist_true_am_Spielart_Schritt_mit_Reizwert()
    {
        var vm = ForGame();
        vm.Bid = "200";

        vm.CanAdvance.Should().BeTrue();
    }

    [Fact]
    public void CanAdvance_blockiert_GoNext_wenn_false()
    {
        var vm = ForGame();
        vm.GoNext(); // kein Bid → CanAdvance=false → bleibt bei Spielart

        vm.CurrentStep.Should().Be(FormStep.Spielart);
    }

    [Fact]
    public void CanAdvance_ist_true_am_Melden_Schritt()
    {
        var vm = ForGame();
        vm.Bid = "200";
        vm.GoNext(); // → Melden

        vm.CurrentStep.Should().Be(FormStep.Melden);
        vm.CanAdvance.Should().BeTrue();
    }

    [Fact]
    public void CanAdvance_ist_false_am_Stiche_Schritt_wenn_Summe_ungleich_Maximum()
    {
        var vm = ForGame();
        vm.Bid = "200";
        vm.GoNext(); // → Reizwert
        vm.GoNext(); // → Melden
        vm.GoNext(); // → Stiche

        vm.CurrentStep.Should().Be(FormStep.Stiche);
        vm.Tricks[0] = "70"; vm.Tricks[1] = "80"; vm.Tricks[2] = "80"; // 230 ≠ 240
        vm.CanAdvance.Should().BeFalse();
    }

    [Fact]
    public void CanAdvance_ist_true_am_Stiche_Schritt_wenn_Summe_gleich_Maximum()
    {
        var vm = ForGame();
        vm.Bid = "200";
        vm.GoNext(); // → Reizwert
        vm.GoNext(); // → Melden
        vm.GoNext(); // → Stiche

        vm.CurrentStep.Should().Be(FormStep.Stiche);
        vm.Tricks[0] = "100"; vm.Tricks[1] = "100"; vm.Tricks[2] = "40"; // 240 = 240
        vm.CanAdvance.Should().BeTrue();
    }

    [Fact]
    public void TricksSum_und_MaxTricksTotal_werden_korrekt_berechnet()
    {
        var vm = ForGame(); // Standardregeln: 8×(11+10+4+3+2) = 240 (ohne Letzter-Stich-Bonus)
        vm.Tricks[0] = "100"; vm.Tricks[1] = "80"; vm.Tricks[2] = "60";

        vm.TricksSum.Should().Be(240);
        vm.MaxTricksTotal.Should().Be(240);
        vm.TricksSumValid.Should().BeTrue();
    }

    [Fact]
    public void TricksSumDisplay_und_MaxTricksTotalDisplay_enthalten_LastTrickBonus()
    {
        var vm = ForGame(); // Standard: LastTrickBonus = 10
        vm.Tricks[0] = "100"; vm.Tricks[1] = "80"; vm.Tricks[2] = "60"; // Summe = 240

        vm.TricksSumDisplay.Should().Be(250);    // 240 + 10
        vm.MaxTricksTotalDisplay.Should().Be(250);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ToggleAbgegangen — Stiche werden beim Abgang geleert
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToggleAbgegangen_leert_alle_Stiche_wenn_Reizer_abgeht()
    {
        var vm = ForGame();
        vm.SetTrick(0, "100");
        vm.SetTrick(1, "80"); // auto-fills field 2 = 60

        vm.ToggleAbgegangen(0); // Bidder = 0 folds

        vm.Tricks[0].Should().Be("");
        vm.Tricks[1].Should().Be("");
        vm.Tricks[2].Should().Be("");
        vm.TricksSum.Should().Be(0);
    }

    [Fact]
    public void ToggleAbgegangen_BuildRound_setzt_Abgegangen_korrekt_wenn_Stiche_vorher_eingegeben()
    {
        // Regression: tricks were auto-filled, then bidder folds → should still be recorded as trueAbgang
        var vm = ForGame();
        vm.Bid = "200";
        vm.SetTrick(0, "100");
        vm.SetTrick(1, "80"); // auto-fills field 2 = 60

        vm.ToggleAbgegangen(0); // Bidder folds — tricks must be cleared

        var round = vm.BuildRound();

        round.PlayerScores[0].Abgegangen.Should().BeTrue();
        round.PlayerScores[0].Tricks.Should().Be(0);
        round.PlayerScores[1].Tricks.Should().Be(0);
        round.PlayerScores[2].Tricks.Should().Be(0);
    }

    [Fact]
    public void ToggleAbgegangen_leert_Stiche_nicht_wenn_Mitspieler_abgeht()
    {
        var vm = ForGame(Build.Rules.AllCanAbgehen());
        vm.SetBidder(0);
        vm.SetTrick(0, "100");
        vm.SetTrick(1, "80"); // auto-fills field 2 = 60

        vm.ToggleAbgegangen(1); // Mitspieler folds, NOT bidder → tricks must stay

        vm.Tricks[0].Should().Be("100");
        vm.Tricks[1].Should().Be("80");
        vm.Tricks[2].Should().Be("60");
    }

    // ══════════════════════════════════════════════════════════════════════
    // SetTrick — Auto-Berechnung des dritten Stich-Werts
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetTrick_berechnet_dritten_Wert_wenn_zwei_eingegeben()
    {
        var vm = ForGame(); // MaxTricksTotal = 240

        vm.SetTrick(0, "100");
        vm.SetTrick(1, "80");

        vm.Tricks[2].Should().Be("60"); // 240 - 100 - 80 = 60
    }

    [Fact]
    public void SetTrick_berechnet_ersten_Wert_wenn_zwei_und_drei_eingegeben()
    {
        var vm = ForGame();

        vm.SetTrick(1, "90");
        vm.SetTrick(2, "70");

        vm.Tricks[0].Should().Be("80"); // 240 - 90 - 70 = 80
    }

    [Fact]
    public void SetTrick_berechnet_zweiten_Wert_wenn_eins_und_drei_eingegeben()
    {
        var vm = ForGame();

        vm.SetTrick(0, "120");
        vm.SetTrick(2, "60");

        vm.Tricks[1].Should().Be("60"); // 240 - 120 - 60 = 60
    }

    [Fact]
    public void SetTrick_macht_kein_AutoFill_wenn_nur_ein_Wert_eingegeben()
    {
        var vm = ForGame();

        vm.SetTrick(0, "100");

        vm.Tricks[1].Should().Be("");
        vm.Tricks[2].Should().Be("");
    }

    [Fact]
    public void SetTrick_laesst_Feld_leer_wenn_Ergebnis_negativ_waere()
    {
        var vm = ForGame();

        vm.SetTrick(0, "180");
        vm.SetTrick(1, "100"); // 240 - 180 - 100 = -40

        vm.Tricks[2].Should().Be(""); // negativer Wert → leer lassen
    }

    [Fact]
    public void SetTrick_kein_AutoFill_bei_vier_Spielern()
    {
        var game = Build.Game(new[] { "A", "B", "C", "D" }, Build.Rules.TeamMode());
        var vm   = new AddRoundViewModel(game);

        vm.SetTrick(0, "100");
        vm.SetTrick(1, "80");
        vm.SetTrick(2, "30");

        vm.Tricks[3].Should().Be(""); // 4 Spieler → kein Auto-Fill
    }

    [Fact]
    public void AutoFill_Summe_ergibt_MaxTricksTotal()
    {
        var vm = ForGame();

        vm.SetTrick(0, "130");
        vm.SetTrick(2, "50");

        int.TryParse(vm.Tricks[1], out var auto).Should().BeTrue();
        (130 + auto + 50).Should().Be(vm.MaxTricksTotal); // Wert1 + AutoWert + Wert2 = 240
    }

    [Fact]
    public void SetTrick_aktualisiert_AutoFill_wenn_Eingabe_zeichenweise_erfolgt()
    {
        // Regression: typing "1" triggers auto-fill with wrong value;
        // completing "15" must recalculate — not leave the stale 224.
        var vm = ForGame();

        vm.SetTrick(0, "15");
        vm.SetTrick(1, "1");   // intermediate keystroke → auto-fills field 2 with 224
        vm.SetTrick(1, "15");  // final value → field 2 must update to 210

        vm.Tricks[2].Should().Be("210"); // 240 - 15 - 15 = 210
    }

    [Fact]
    public void SetTrick_stoppt_AutoFill_wenn_Nutzer_AutoFill_Feld_manuell_editiert()
    {
        var vm = ForGame();

        vm.SetTrick(0, "100");
        vm.SetTrick(1, "80");   // field 2 auto-filled to 60

        vm.SetTrick(2, "70");   // user overrides auto-fill → tracking stops

        // Now changing field 0 must NOT recalculate field 2
        vm.SetTrick(0, "90");
        vm.Tricks[2].Should().Be("70");
    }

    // ══════════════════════════════════════════════════════════════════════
    // Anzeigehelfer
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void BidderLabel_ist_Spieler_fuer_Durch_und_Reizer_fuer_Normal()
    {
        var vm = new AddRoundViewModel(Build.Game(new[] { "A", "B", "C" }, Build.Rules.WithDurch()));

        vm.BidderLabel.Should().Be("Reizer");  // Normal ist default

        vm.SetType(RoundType.Durch);
        vm.BidderLabel.Should().Be("Spieler");
    }

    [Fact]
    public void ShowGameTypeSelector_ist_true_wenn_Durch_oder_Bettel_erlaubt()
    {
        var normal = ForGame(Build.Rules.Default());
        var durch  = ForGame(Build.Rules.WithDurch());

        normal.ShowGameTypeSelector.Should().BeFalse();
        durch.ShowGameTypeSelector.Should().BeTrue();
    }

    [Fact]
    public void PlayerRowClass_gibt_bidder_highlight_fuer_Reizer()
    {
        var vm = ForGame();
        vm.SetBidder(1);

        vm.PlayerRowClass(0).Should().Be("player-row");
        vm.PlayerRowClass(1).Should().Be("bidder-highlight");
        vm.PlayerRowClass(2).Should().Be("player-row");
    }

    [Fact]
    public void ShowAbgehenFor_zeigt_nur_Reizer_wenn_BidderOnlyAbgehen_aktiv()
    {
        var vm = ForGame(Build.Rules.Default()); // BidderOnlyAbgehen = true

        vm.ShowAbgehenFor(0).Should().BeTrue();   // Reizer
        vm.ShowAbgehenFor(1).Should().BeFalse();  // Mitspieler
    }

    [Fact]
    public void ShowAbgehenFor_zeigt_alle_wenn_BidderOnlyAbgehen_deaktiviert()
    {
        var vm = ForGame(Build.Rules.AllCanAbgehen());

        vm.ShowAbgehenFor(0).Should().BeTrue();
        vm.ShowAbgehenFor(1).Should().BeTrue();
        vm.ShowAbgehenFor(2).Should().BeTrue();
    }

    [Fact]
    public void SpecialResultLabel_unterscheidet_Durch_und_Bettel()
    {
        var vm = new AddRoundViewModel(
            Build.Game(new[] { "A", "B", "C" }, Build.Rules.WithDurch().With(r => r.AllowBettel = true)));

        vm.SetType(RoundType.Durch);
        vm.SpecialResultLabel.Should().Be("Alle Stiche gemacht?");

        vm.SetType(RoundType.Bettel);
        vm.SpecialResultLabel.Should().Be("Keinen Stich gemacht?");
    }

    [Fact]
    public void DurchPointsDisplay_hat_Plus_wenn_gewonnen()
    {
        var vm = ForGame(Build.Rules.WithDurch());
        vm.SetType(RoundType.Durch);
        vm.Won = true;

        vm.DurchPointsDisplay.Should().StartWith("+");
    }

    // ══════════════════════════════════════════════════════════════════════
    // BuildRound
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void BuildRound_erzeugt_korrekte_normale_Runde()
    {
        var vm = ForGame();
        vm.SetBidder(1);
        vm.Bid       = "250";
        vm.Meld[0]   = "100"; vm.Meld[1]   = "200"; vm.Meld[2]   = "50";
        vm.Tricks[0] = "80";  vm.Tricks[1]  = "120"; vm.Tricks[2] = "30";

        var round = vm.BuildRound();

        round.Type.Should().Be(RoundType.Normal);
        round.Bidder.Should().Be(1);
        round.Bid.Should().Be(250);
        round.PlayerScores[1].Meld.Should().Be(200);
        round.PlayerScores[1].Tricks.Should().Be(120);
    }

    [Fact]
    public void BuildRound_setzt_Reizer_Meld_und_alle_Stiche_auf_null_wenn_vor_Spiel_abgegangen()
    {
        // trueAbgang: Reizer abgegangen BEVOR das Spiel gespielt wurde (TricksSum == 0)
        var vm = ForGame();
        vm.Bid     = "300";
        vm.Meld[0] = "100"; // Meld wurde eingegeben, aber keine Stiche
        vm.ToggleAbgegangen(0);

        var round = vm.BuildRound();

        round.PlayerScores[0].Meld.Should().Be(0);       // Reizer Meld gelöscht (trueAbgang)
        round.PlayerScores[0].Tricks.Should().Be(0);
        round.PlayerScores[1].Tricks.Should().Be(0);
        round.PlayerScores[2].Tricks.Should().Be(0);
        round.PlayerScores[0].Abgegangen.Should().BeTrue();
    }

[Fact]
    public void BuildRound_Won_ist_true_fuer_Durch_wenn_Won_gesetzt()
    {
        var vm = new AddRoundViewModel(Build.Game(new[] { "A", "B", "C" }, Build.Rules.WithDurch()));
        vm.SetType(RoundType.Durch);
        vm.Won = true;

        var round = vm.BuildRound();

        round.Type.Should().Be(RoundType.Durch);
        round.Won.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════
    // GetScorePreviews
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetScorePreviews_zeigt_Abgegangen_Verlust_fuer_Reizer()
    {
        var vm = ForGame();
        vm.Bid = "300";
        vm.ToggleAbgegangen(0);

        var previews = vm.GetScorePreviews();

        previews[0].IsLoss.Should().BeTrue();
        previews[0].FinalScore.Should().Be(-300);
        previews[0].LossReason.Should().Be("Abgegangen");
    }

    [Fact]
    public void GetScorePreviews_zeigt_Bonus_fuer_Mitspieler_wenn_Reizer_abgegangen()
    {
        var vm = ForGame();
        vm.Bid     = "300";
        vm.Meld[1] = "100";
        vm.ToggleAbgegangen(0);

        var previews = vm.GetScorePreviews();

        previews[1].Bonus.Should().Be(30);       // 3 Spieler × 10
        previews[1].FinalScore.Should().Be(130); // 100 Meld + 30 Bonus
    }
}

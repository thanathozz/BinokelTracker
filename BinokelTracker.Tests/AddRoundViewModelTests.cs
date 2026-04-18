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
    public void Normale_Runde_hat_sechs_Schritte()
    {
        var vm = ForGame();

        vm.ActiveSteps.Should().Equal(
            FormStep.Spielart, FormStep.Reizwert, FormStep.Melden,
            FormStep.Stiche,   FormStep.LetzterStich, FormStep.Ergebnis);
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
        vm.GoNext();           // → Reizwert
        vm.Bid = "300";
        vm.ToggleAbgegangen(0); // Reizer abgegangen

        vm.ActiveSteps.Should().Equal(
            FormStep.Spielart, FormStep.Reizwert, FormStep.Melden, FormStep.Ergebnis);
        vm.TotalSteps.Should().Be(4);
    }

    [Fact]
    public void Reizer_abgegangen_zurueckgenommen_stellt_Stiche_wieder_her()
    {
        var vm = ForGame();
        vm.GoNext();
        vm.Bid = "300";
        vm.ToggleAbgegangen(0);  // abgegangen
        vm.ToggleAbgegangen(0);  // wieder zurück

        vm.TotalSteps.Should().Be(6);
        vm.ActiveSteps.Should().Contain(FormStep.Stiche);
        vm.ActiveSteps.Should().Contain(FormStep.LetzterStich);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Navigation (GoNext / GoPrev)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GoNext_wechselt_zum_naechsten_Schritt()
    {
        var vm = ForGame();
        vm.GoNext();

        vm.Step.Should().Be(1);
        vm.CurrentStep.Should().Be(FormStep.Reizwert);
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
        vm.GoNext();
        vm.Bid = "200";
        vm.GoNext();

        vm.GoPrev();

        vm.Step.Should().Be(1);
        vm.CurrentStep.Should().Be(FormStep.Reizwert);
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
        vm.GoNext();
        vm.Forward.Should().BeTrue();

        vm.GoPrev();
        vm.Forward.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════════
    // CanAdvance
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanAdvance_ist_false_am_Reizwert_Schritt_ohne_Eingabe()
    {
        var vm = ForGame();
        vm.GoNext(); // → Reizwert

        vm.CurrentStep.Should().Be(FormStep.Reizwert);
        vm.CanAdvance.Should().BeFalse();
    }

    [Fact]
    public void CanAdvance_ist_true_am_Reizwert_Schritt_mit_Eingabe()
    {
        var vm = ForGame();
        vm.GoNext();
        vm.Bid = "200";

        vm.CanAdvance.Should().BeTrue();
    }

    [Fact]
    public void CanAdvance_blockiert_GoNext_wenn_false()
    {
        var vm = ForGame();
        vm.GoNext(); // → Reizwert (kein Bid → CanAdvance=false)
        vm.GoNext(); // soll nichts tun

        vm.CurrentStep.Should().Be(FormStep.Reizwert);
    }

    [Fact]
    public void CanAdvance_ist_immer_true_ausserhalb_des_Reizwert_Schritts()
    {
        var vm = ForGame();

        // Schritt 0 (Spielart): kein Block
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
    public void BuildRound_setzt_Reizer_Meld_und_alle_Stiche_auf_null_wenn_abgegangen()
    {
        var vm = ForGame();
        vm.Bid       = "300";
        vm.Meld[0]   = "100";
        vm.Tricks[0] = "50";
        vm.ToggleAbgegangen(0);

        var round = vm.BuildRound();

        round.PlayerScores[0].Meld.Should().Be(0);       // Reizer Meld gelöscht
        round.PlayerScores[0].Tricks.Should().Be(0);
        round.PlayerScores[1].Tricks.Should().Be(0);     // alle Stiche = 0
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

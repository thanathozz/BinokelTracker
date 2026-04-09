using BinokelTracker.Models;
using BinokelTracker.Services;
using FluentAssertions;

namespace BinokelTracker.Tests;

/// <summary>
/// Testet die Statistik-Berechnung: Spieler-Zähler, Gewinn-Ermittlung,
/// Geldbilanz und Sortierung.
/// </summary>
public class StatisticsServiceTests
{
    // ══════════════════════════════════════════════════════════════════════
    // Bieter-Zähler (normale Runden)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Reizer_Sieg_wird_als_BidderWin_gezaehlt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(
            bidder: 0, bid: 200,
            meld:   new[] { 200, 0, 0 },
            tricks: new[] { 100, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.RoundsAsBidder.Should().Be(1);
        anna.BidderWins.Should().Be(1);
        anna.BidderLosses.Should().Be(0);
        anna.BidderAbgegangen.Should().Be(0);
    }

    [Fact]
    public void Reizer_Niederlage_wird_als_BidderLoss_gezaehlt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(
            bidder: 0, bid: 500,
            meld:   new[] { 0, 0, 0 },
            tricks: new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.BidderLosses.Should().Be(1);
        anna.BidderWins.Should().Be(0);
    }

    [Fact]
    public void Abgehen_zaehlt_als_BidderAbgegangen_nicht_als_Win_oder_Loss()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(
            bidder: 0, bid: 300,
            meld:   new[] { 0, 0, 0 },
            tricks: new[] { 0, 0, 0 },
            abgegangen: new[] { true, false, false }));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.BidderAbgegangen.Should().Be(1);
        anna.BidderWins.Should().Be(0);
        anna.BidderLosses.Should().Be(0);
    }

    [Fact]
    public void Durch_zaehlt_separat_nicht_als_normale_Bieterrunde()
    {
        var rules = Build.Rules.WithDurch();
        var game  = Build.Game(new[] { "Anna", "Bob", "Carl" }, rules);
        game.Rounds.Add(Build.DurchRound(bidder: 0, won: true));
        game.Rounds.Add(Build.DurchRound(bidder: 0, won: false));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.DurchPlayed.Should().Be(2);
        anna.DurchWon.Should().Be(1);
        anna.RoundsAsBidder.Should().Be(0);  // Durch ist keine normale Bieterrunde
        anna.BidderWins.Should().Be(0);
    }

    [Fact]
    public void Bettel_zaehlt_separat_und_nicht_als_Durch()
    {
        var rules = Build.Rules.WithBettel();
        var game  = Build.Game(new[] { "Anna", "Bob", "Carl" }, rules);
        game.Rounds.Add(Build.BettelRound(bidder: 1, won: true));

        var stats = StatisticsService.Compute(new[] { game });
        var bob   = stats.Players.Single(p => p.Name == "Bob");

        bob.BettelPlayed.Should().Be(1);
        bob.BettelWon.Should().Be(1);
        bob.DurchPlayed.Should().Be(0);
    }

    [Fact]
    public void Mehrere_Spiele_addieren_Statistiken_pro_Spieler()
    {
        var game1 = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game1.Rounds.Add(Build.NormalRound(0, 200, new[] { 200, 0, 0 }, new[] { 100, 0, 0 }));

        var game2 = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game2.Rounds.Add(Build.NormalRound(0, 300, new[] { 200, 0, 0 }, new[] { 50, 0, 0 })); // verliert

        var stats = StatisticsService.Compute(new[] { game1, game2 });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.RoundsAsBidder.Should().Be(2);
        anna.BidderWins.Should().Be(1);
        anna.BidderLosses.Should().Be(1);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Spiel-Gewinner
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GamesWon_zaehlt_Spieler_mit_hoechstem_Punktestand_bei_beendetem_Spiel()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.Single(p => p.Name == "Anna").GamesWon.Should().Be(1);
        stats.Players.Single(p => p.Name == "Bob").GamesWon.Should().Be(0);
    }

    [Fact]
    public void GamesWon_wird_nicht_bei_laufendem_Spiel_gezaehlt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: false);
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.Single(p => p.Name == "Anna").GamesWon.Should().Be(0);
    }

    [Fact]
    public void Bei_Gleichstand_gewinnen_alle_fuehrenden_Spieler()
    {
        // Anna und Bob haben beide 300, Carl nichts
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game.Rounds.Add(Build.NormalRound(0, 300, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(1, 300, new[] { 0, 300, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.Single(p => p.Name == "Anna").GamesWon.Should().Be(1);
        stats.Players.Single(p => p.Name == "Bob").GamesWon.Should().Be(1);
        stats.Players.Single(p => p.Name == "Carl").GamesWon.Should().Be(0);
    }

    [Fact]
    public void QuickGame_Gewinner_wird_korrekt_erkannt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game.QuickWinner = 2; // Carl

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.Single(p => p.Name == "Carl").GamesWon.Should().Be(1);
        stats.Players.Single(p => p.Name == "Anna").GamesWon.Should().Be(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Geldbilanz (Einsatz)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Gewinner_erhaelt_Einsatz_mal_Anzahl_Verlierer()
    {
        // 3 Spieler, 1 Gewinner → +2 * Einsatz
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true, einsatz: "5");
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.Single(p => p.Name == "Anna").MoneyBalance.Should().Be(10); // 2 * 5
        stats.Players.Single(p => p.Name == "Bob").MoneyBalance.Should().Be(-5);
        stats.Players.Single(p => p.Name == "Carl").MoneyBalance.Should().Be(-5);
    }

    [Fact]
    public void Geldbilanz_ist_null_bei_laufendem_Spiel()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: false, einsatz: "10");
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.Single(p => p.Name == "Anna").MoneyBalance.Should().Be(0);
    }

    [Fact]
    public void Geldbilanz_ist_null_wenn_kein_Einsatz_gesetzt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true, einsatz: "0");
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 300, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.All(p => p.MoneyBalance == 0).Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Abgeleitete Kennzahlen
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void BidderWinRate_ist_null_wenn_nie_gereizt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 200, 0, 0 }, new[] { 100, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });
        var bob   = stats.Players.Single(p => p.Name == "Bob");

        bob.BidderWinRate.Should().Be(0);
    }

    [Fact]
    public void BidderWinRate_berechnet_Siegquote_korrekt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        // 2 Runden: 1 Sieg, 1 Niederlage → 50 %
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 200, 0, 0 }, new[] { 100, 0, 0 })); // gewinnt
        game.Rounds.Add(Build.NormalRound(0, 500, new[] {   0, 0, 0 }, new[] {   0, 0, 0 })); // verliert

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.BidderWinRate.Should().BeApproximately(50.0, precision: 0.01);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Globale Zähler
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TotalGames_und_FinishedGames_werden_korrekt_gezaehlt()
    {
        var finished = Build.Game(new[] { "A", "B", "C" }, finished: true);
        var running  = Build.Game(new[] { "A", "B", "C" }, finished: false);

        var stats = StatisticsService.Compute(new[] { finished, running });

        stats.TotalGames.Should().Be(2);
        stats.FinishedGames.Should().Be(1);
    }

    [Fact]
    public void TotalRounds_zaehlt_Runden_ueber_alle_Spiele()
    {
        var game1 = Build.Game(new[] { "A", "B", "C" });
        game1.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 }));
        game1.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 }));

        var game2 = Build.Game(new[] { "A", "B", "C" });
        game2.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game1, game2 });

        stats.TotalRounds.Should().Be(3);
    }
}

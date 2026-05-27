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
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));

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
        game.Rounds.Add(Build.NormalRound(0, 300, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(1, 300, new[] { 0, 0, 0 }, new[] { 0, 300, 0 }));

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
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));

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

    // ══════════════════════════════════════════════════════════════════════
    // Spiel-Scoring (HighestGameScore, LowestGameScore, AvgFinalScore)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HighestGameScore_gibt_hoechsten_Endpunktestand_aus_beendeten_Spielen()
    {
        var game1 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game1.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));

        var game2 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game2.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 500, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game1, game2 });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.HighestGameScore.Should().Be(500);
    }

    [Fact]
    public void HighestGameScore_ignoriert_laufende_Spiele()
    {
        // Anna gewinnt das beendete Spiel mit 300 Punkten (bid=200, tricks=300 ≥ bid → score=300)
        var finished = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        finished.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));

        // Laufendes Spiel mit noch höheren Tricks – darf nicht mitzählen
        var running = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: false);
        running.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 999, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { finished, running });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.HighestGameScore.Should().Be(300);
    }

    [Fact]
    public void LowestGameScore_ignoriert_Spiele_ohne_Runden()
    {
        // Anna gewinnt beide Spiele mit Runden (Scores 200 und 300)
        var game1 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game1.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 100, 0, 0 }));
        // Anna: bid=100, meld=100, tricks=100, total=200 ≥ 100 → Score=200

        var game2 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game2.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 200, 0, 0 }));
        // Anna: bid=100, meld=100, tricks=200, total=300 ≥ 100 → Score=300

        // Beendetes Spiel ohne Runden: totals = [0,0,0] für alle – soll LowestGameScore nicht auf 0 drücken
        var ohneRunden = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);

        var stats = StatisticsService.Compute(new[] { game1, game2, ohneRunden });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.LowestGameScore.Should().Be(200);
    }

    [Fact]
    public void AvgFinalScore_berechnet_Durchschnitt_ueber_beendete_Spiele()
    {
        var game1 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game1.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 100, 0, 0 }));

        var game2 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game2.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game1, game2 });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.AvgFinalScore.Should().BeApproximately(200.0, 0.01);
    }

    // ══════════════════════════════════════════════════════════════════════
    // LongestWinStreak
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void LongestWinStreak_zaehlt_aufeinanderfolgende_Siege_chronologisch()
    {
        // Anna gewinnt Spiel 1 (Date=1), verliert Spiel 2 (Date=2), gewinnt Spiel 3+4 (Date=3,4)
        var game1 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game1.Date = 1;
        game1.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 500, 0, 0 }));

        var game2 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game2.Date = 2;
        game2.Rounds.Add(Build.NormalRound(1, 100, new[] { 0, 0, 0 }, new[] { 0, 500, 0 }));

        var game3 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game3.Date = 3;
        game3.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 500, 0, 0 }));

        var game4 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game4.Date = 4;
        game4.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 500, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game1, game2, game3, game4 });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.LongestWinStreak.Should().Be(2);
    }

    [Fact]
    public void LongestWinStreak_ist_null_ohne_beendete_Spiele()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: false);
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 500, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.LongestWinStreak.Should().Be(0);
    }

    // ══════════════════════════════════════════════════════════════════════
    // HighestBidEver
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HighestBidEver_gibt_hoechsten_Reizwert_inkl_Abgegangen()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(0, 300, new[] { 0, 0, 0 }, new[] { 400, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(0, 500, new[] { 0, 0, 0 }, new[] { 0, 0, 0 },
            abgegangen: new[] { true, false, false }));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.HighestBidEver.Should().Be(500);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Meld & Tricks (Normalen Runden)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NormalRoundsPlayed_zaehlt_alle_Spieler_in_normalen_Runden()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(0, 200, new[] { 200, 0, 0 }, new[] { 100, 50, 30 }));
        game.Rounds.Add(Build.NormalRound(1, 200, new[] { 0, 200, 0 }, new[] { 50, 100, 30 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.Single(p => p.Name == "Anna").NormalRoundsPlayed.Should().Be(2);
        stats.Players.Single(p => p.Name == "Bob").NormalRoundsPlayed.Should().Be(2);
        stats.Players.Single(p => p.Name == "Carl").NormalRoundsPlayed.Should().Be(2);
    }

    [Fact]
    public void AvgMeldPoints_berechnet_Durchschnitt_ueber_normale_Runden()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 200, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.AvgMeldPoints.Should().BeApproximately(150.0, 0.01);
    }

    [Fact]
    public void AvgTrickPoints_und_MeldToTrickRatio_korrekt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 60, 0, 0 }, new[] { 120, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.AvgMeldPoints.Should().BeApproximately(60.0, 0.01);
        anna.AvgTrickPoints.Should().BeApproximately(120.0, 0.01);
        anna.MeldToTrickRatio.Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void MeldToTrickRatio_ist_null_wenn_keine_Stiche()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.MeldToTrickRatio.Should().Be(0);
    }

    [Fact]
    public void LastTrickWins_und_LastTrickWinRate_korrekt()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 },
            lastTrickWinner: 0));
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 },
            lastTrickWinner: 1));

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.LastTrickWins.Should().Be(1);
        anna.LastTrickWinRate.Should().BeApproximately(50.0, 0.01);
    }

    [Fact]
    public void HighestMeldInRound_gibt_hoechste_Meldpunkte_einer_Runde()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 150, 40, 20 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(1, 100, new[] { 60, 200, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.Players.Single(p => p.Name == "Anna").HighestMeldInRound.Should().Be(150);
        stats.Players.Single(p => p.Name == "Bob").HighestMeldInRound.Should().Be(200);
    }

    [Fact]
    public void MeldTypeFrequency_zaehlt_nur_Runden_mit_gesetzten_MeldTypes()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        game.Rounds.Add(Build.NormalRound(
            0, 100, new[] { 40, 0, 0 }, new[] { 0, 0, 0 },
            meldTypes: new[] { new List<MeldType> { MeldType.Binokel }, null, null }));
        game.Rounds.Add(Build.NormalRound(
            0, 100, new[] { 40, 0, 0 }, new[] { 0, 0, 0 })); // MeldTypes null → nicht gezählt

        var stats = StatisticsService.Compute(new[] { game });
        var anna  = stats.Players.Single(p => p.Name == "Anna");

        anna.MeldTypeFrequency.Should().ContainKey(MeldType.Binokel)
            .WhoseValue.Should().Be(1);
        anna.MeldTypeFrequency.Count.Should().Be(1);
    }

    // ══════════════════════════════════════════════════════════════════════
    // RoundStats
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TrumpFrequency_zaehlt_Trumpffarben_in_normalen_Runden()
    {
        var game = Build.Game(new[] { "A", "B", "C" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 },
            trumpf: TrumpSuit.Herz));
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 },
            trumpf: TrumpSuit.Herz));
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 },
            trumpf: TrumpSuit.Eichel));

        var stats = StatisticsService.Compute(new[] { game });

        stats.RoundStats.TrumpFrequency[TrumpSuit.Herz].Should().Be(2);
        stats.RoundStats.TrumpFrequency[TrumpSuit.Eichel].Should().Be(1);
        stats.RoundStats.TrumpFrequency.ContainsKey(TrumpSuit.Schippen).Should().BeFalse();
    }

    [Fact]
    public void AvgTotalPointsPerRound_ist_Durchschnitt_aller_Spielerpunkte_je_Runde()
    {
        var game = Build.Game(new[] { "A", "B", "C" });
        // Runde 1: 100+50+30 = 180
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 50, 30 }, new[] { 0, 0, 0 }));
        // Runde 2: 0+0+0 = 0
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.RoundStats.AvgTotalPointsPerRound.Should().BeApproximately(90.0, 0.01);
    }

    [Fact]
    public void RoundWinnerCount_zaehlt_Nicht_Bieter_mit_positivem_Score()
    {
        var game = Build.Game(new[] { "Anna", "Bob", "Carl" });
        // Anna reizt, Bob und Carl punkten als Nicht-Bieter
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 50, 30 }, new[] { 0, 30, 20 }));
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 50, 0  }, new[] { 0, 20, 0  }));

        var stats = StatisticsService.Compute(new[] { game });

        stats.RoundStats.RoundWinnerCount["Bob"].Should().Be(2);
        stats.RoundStats.RoundWinnerCount["Carl"].Should().Be(1);
        stats.RoundStats.RoundWinnerCount.ContainsKey("Anna").Should().BeFalse();
    }

    [Fact]
    public void TrumpFrequency_ignoriert_Durch_und_Bettel_Runden()
    {
        var rules = Build.Rules.WithDurch();
        var game  = Build.Game(new[] { "A", "B", "C" }, rules);
        game.Rounds.Add(Build.DurchRound(0, true));
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 100, 0, 0 }, new[] { 0, 0, 0 },
            trumpf: TrumpSuit.Schellen));

        var stats = StatisticsService.Compute(new[] { game });

        stats.RoundStats.TrumpFrequency.Should().HaveCount(1);
        stats.RoundStats.TrumpFrequency[TrumpSuit.Schellen].Should().Be(1);
    }

    // ══════════════════════════════════════════════════════════════════════
    // HeadToHead
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HeadToHead_zaehlt_geteilte_Spiele_und_Siege_korrekt()
    {
        // Spiel 1: Anna 500, Bob 0 → Anna gewinnt
        var game1 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game1.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 500, 0, 0 }));

        // Spiel 2: Anna 0, Bob 500 → Bob gewinnt
        var game2 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game2.Rounds.Add(Build.NormalRound(1, 200, new[] { 0, 0, 0 }, new[] { 0, 500, 0 }));

        // Spiel 3: Anna 300, Bob 300 → Unentschieden
        var game3 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game3.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));
        game3.Rounds.Add(Build.NormalRound(1, 200, new[] { 0, 0, 0 }, new[] { 0, 300, 0 }));

        var stats  = StatisticsService.Compute(new[] { game1, game2, game3 });
        var annaBob = stats.HeadToHead
            .Single(h => (h.PlayerA == "Anna" && h.PlayerB == "Bob") ||
                         (h.PlayerA == "Bob"  && h.PlayerB == "Anna"));

        annaBob.GamesShared.Should().Be(3);
        annaBob.Ties.Should().Be(1);
        (annaBob.AWins + annaBob.BWins).Should().Be(2);
    }

    [Fact]
    public void HeadToHead_ignoriert_laufende_Spiele()
    {
        var finished = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        finished.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));

        var running = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: false);
        running.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 300, 0, 0 }));

        var stats = StatisticsService.Compute(new[] { finished, running });
        var annaBob = stats.HeadToHead
            .Single(h => (h.PlayerA == "Anna" && h.PlayerB == "Bob") ||
                         (h.PlayerA == "Bob"  && h.PlayerB == "Anna"));

        annaBob.GamesShared.Should().Be(1);
    }

    [Fact]
    public void HeadToHead_AvgPointDiff_ist_Durchschnitt_der_Punktedifferenz()
    {
        // Spiel 1: A=400, B=100 → diff = +300 (wenn A alphabetisch vor B)
        var game1 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game1.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 400, 0, 0 }));
        game1.Rounds.Add(Build.NormalRound(1, 200, new[] { 0, 0, 0 }, new[] { 0, 100, 0 }));

        // Spiel 2: A=100, B=300 → diff = -200
        var game2 = Build.Game(new[] { "Anna", "Bob", "Carl" }, finished: true);
        game2.Rounds.Add(Build.NormalRound(0, 200, new[] { 0, 0, 0 }, new[] { 100, 0, 0 }));
        game2.Rounds.Add(Build.NormalRound(1, 200, new[] { 0, 0, 0 }, new[] { 0, 300, 0 }));

        var stats  = StatisticsService.Compute(new[] { game1, game2 });
        var annaBob = stats.HeadToHead
            .Single(h => (h.PlayerA == "Anna" && h.PlayerB == "Bob") ||
                         (h.PlayerA == "Bob"  && h.PlayerB == "Anna"));

        // avg diff should be (300 + (-200)) / 2 = 50 (A=Anna is alphabetically before B=Bob)
        annaBob.AvgPointDiff.Should().BeApproximately(50.0, 0.01);
    }
}

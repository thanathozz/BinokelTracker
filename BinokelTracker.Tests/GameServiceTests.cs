using BinokelTracker.Models;
using BinokelTracker.Services;
using FluentAssertions;

namespace BinokelTracker.Tests;

/// <summary>
/// Testet den GameService: Auto-Finish, Runden-Verwaltung, Spieler-Registrierung.
/// </summary>
public class GameServiceTests
{
    private readonly GameService _svc = new();

    // ══════════════════════════════════════════════════════════════════════
    // AddRound — Zielwert und Auto-Finish
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Spiel_schliesst_automatisch_wenn_Zielwert_erreicht()
    {
        var rules = Build.Rules.Default().With(r => r.TargetScore = 300);
        var game  = Build.Game(new[] { "A", "B", "C" }, rules);
        var round = Build.NormalRound(0, 200, new[] { 250, 0, 0 }, new[] { 100, 0, 0 });

        _svc.AddRound(game, round);

        game.Finished.Should().BeTrue();
        game.Rounds.Should().HaveCount(1);
    }

    [Fact]
    public void Spiel_bleibt_offen_wenn_Zielwert_nicht_erreicht()
    {
        var rules = Build.Rules.Default().With(r => r.TargetScore = 1000);
        var game  = Build.Game(new[] { "A", "B", "C" }, rules);
        var round = Build.NormalRound(0, 200, new[] { 200, 0, 0 }, new[] { 100, 0, 0 }); // 300 Punkte

        _svc.AddRound(game, round);

        game.Finished.Should().BeFalse();
    }

    [Fact]
    public void Spiel_schliesst_auch_wenn_ein_Spieler_mit_negativem_Wert_ueber_Ziel_geht()
    {
        // Negativer Zielwert ist unrealistisch, aber positives Ziel: Spieler 1 überschreitet
        var rules = Build.Rules.Default().With(r => r.TargetScore = 500);
        var game  = Build.Game(new[] { "A", "B", "C" }, rules);
        game.Rounds.Add(Build.NormalRound(1, 300, new[] { 0, 300, 0 }, new[] { 0, 200, 0 })); // B: 500

        // Runde direkt hinzufügen, um Auto-Finish zu prüfen
        var nextRound = Build.NormalRound(2, 100, new[] { 0, 0, 100 }, new[] { 0, 0, 100 });
        _svc.AddRound(game, nextRound);

        game.Finished.Should().BeTrue();
    }

    [Fact]
    public void Bereits_beendetes_Spiel_wird_nicht_doppelt_beendet()
    {
        var rules = Build.Rules.Default().With(r => r.TargetScore = 300);
        var game  = Build.Game(new[] { "A", "B", "C" }, rules, finished: true);
        // bereits finished=true — AddRound setzt es nicht zurück
        var round = Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 0, 0, 0 });

        _svc.AddRound(game, round);

        game.Finished.Should().BeTrue(); // bleibt beendet
    }

    // ══════════════════════════════════════════════════════════════════════
    // DeleteRound
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DeleteRound_entfernt_Runde_am_angegebenen_Index()
    {
        var game = Build.Game(new[] { "A", "B", "C" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }));
        game.Rounds.Add(Build.NormalRound(1, 200, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }));

        _svc.DeleteRound(game, 0);

        game.Rounds.Should().HaveCount(1);
        game.Rounds[0].Bidder.Should().Be(1);
    }

    [Fact]
    public void DeleteRound_ignoriert_ungueltigen_Index()
    {
        var game = Build.Game(new[] { "A", "B", "C" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }));

        _svc.DeleteRound(game, 99);

        game.Rounds.Should().HaveCount(1);
    }

    [Fact]
    public void DeleteRound_ignoriert_negativen_Index()
    {
        var game = Build.Game(new[] { "A", "B", "C" });
        game.Rounds.Add(Build.NormalRound(0, 100, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }));

        _svc.DeleteRound(game, -1);

        game.Rounds.Should().HaveCount(1);
    }

    // ══════════════════════════════════════════════════════════════════════
    // RegisterNewPlayers
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RegisterNewPlayers_fuegt_neue_Spieler_hinzu()
    {
        var state = new AppState();

        _svc.RegisterNewPlayers(state, new[] { "Anna", "Bob" });

        state.KnownPlayers.Should().Contain("Anna").And.Contain("Bob");
    }

    [Fact]
    public void RegisterNewPlayers_ignoriert_Duplikate_case_insensitiv()
    {
        var state = new AppState();
        state.KnownPlayers.Add("Anna");

        _svc.RegisterNewPlayers(state, new[] { "anna", "ANNA", "Bob" });

        state.KnownPlayers.Should().HaveCount(2);
        state.KnownPlayers.Should().Contain("Bob");
    }

    [Fact]
    public void RegisterNewPlayers_ignoriert_leere_Namen()
    {
        var state = new AppState();

        _svc.RegisterNewPlayers(state, new[] { "", "  ", "Anna" });

        state.KnownPlayers.Should().HaveCount(1);
        state.KnownPlayers[0].Should().Be("Anna");
    }

    // ══════════════════════════════════════════════════════════════════════
    // FinishGame
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FinishGame_setzt_Finished_auf_true()
    {
        var game = Build.Game(new[] { "A", "B", "C" });

        _svc.FinishGame(game);

        game.Finished.Should().BeTrue();
    }
}

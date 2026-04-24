namespace BinokelTracker.Models;

public class RuleSet
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Players { get; set; } = 3;
    public bool TeamMode { get; set; }
    public int TargetScore { get; set; } = 1000;
    public bool DoubleMinus { get; set; }
    public bool AllowDurch { get; set; }
    public bool AllowBettel { get; set; }
    public bool AllowAbgehen { get; set; } = true;
    public bool BidderOnlyAbgehen { get; set; } = true;
    public int DurchPoints { get; set; } = 1000;
    public int BettelPoints { get; set; } = 1000;
    public bool DurchSeparate { get; set; } = true;
    public int AssValue { get; set; } = 11;
    public int ZehnValue { get; set; } = 10;
    public int KoenigValue { get; set; } = 4;
    public int OberValue { get; set; } = 3;
    public int UnterValue { get; set; } = 2;
    public int LastTrickBonus { get; set; } = 10;
    public int AbgegangenBonusPerPlayer { get; set; } = 10;

    public RuleSet Clone() => (RuleSet)MemberwiseClone();
}

public static class RulePresets
{
    public static readonly Dictionary<string, RuleSet> All = new()
    {
        ["schwaebisch_klassisch"] = new RuleSet
        {
            Id = "schwaebisch_klassisch",
            Name = "Schwäbisch Klassisch",
            Description = "3 Spieler, solo, Ziel 1000, einfach Minus",
            Players = 3, TargetScore = 1000, AllowAbgehen = true
        },
        ["schwaebisch_scharf"] = new RuleSet
        {
            Id = "schwaebisch_scharf",
            Name = "Schwäbisch Scharf",
            Description = "3 Spieler, doppelt Minus bei Überreizen",
            Players = 3, TargetScore = 1000, DoubleMinus = true,
            AllowDurch = true, AllowAbgehen = true, BidderOnlyAbgehen = true
        },
        ["vierer_kreuz"] = new RuleSet
        {
            Id = "vierer_kreuz",
            Name = "Vierer-Kreuz",
            Description = "4 Spieler, 2 Teams, Ziel 1500",
            Players = 4, TeamMode = true, TargetScore = 1500,
            AllowAbgehen = true, BidderOnlyAbgehen = false
        },
        ["turnier"] = new RuleSet
        {
            Id = "turnier",
            Name = "Turnier",
            Description = "3 Spieler, Ziel 1500, mit Durch & Bettel",
            Players = 3, TargetScore = 1500, DoubleMinus = true,
            AllowDurch = true, AllowBettel = true, AllowAbgehen = true, BidderOnlyAbgehen = false
        },
        ["benutzerdefiniert"] = new RuleSet
        {
            Id = "benutzerdefiniert",
            Name = "Benutzerdefiniert",
            Description = "Alle Regeln frei einstellbar",
            Players = 3, AllowDurch = true, AllowBettel = true, AllowAbgehen = true, BidderOnlyAbgehen = false
        }
    };
}

public class PlayerScore
{
    public int Meld { get; set; }
    public int Tricks { get; set; }
    public bool Abgegangen { get; set; }
}

public enum RoundType
{
    Normal,
    Durch,
    Bettel
}

public enum TrumpSuit { Eichel, Gras, Herz, Schellen }

public class Round
{
    public long Id { get; set; }
    public RoundType Type { get; set; } = RoundType.Normal;
    public int Bidder { get; set; }
    public int Bid { get; set; }
    public bool Won { get; set; } = true;
    public TrumpSuit? Trumpf { get; set; }
    public List<PlayerScore> PlayerScores { get; set; } = new();
    /// <summary>Index des Spielers mit dem letzten Stich. -1 = kein Stich gespielt (Reizer abgegangen).</summary>
    public int LastTrickWinner { get; set; } = -1;

    // Computed: true if the bidder abgegangen
    public bool Abgegangen => PlayerScores.Count > Bidder && PlayerScores[Bidder].Abgegangen;

    public int[] CalcScores(RuleSet rules)
    {
        var scores = new int[PlayerScores.Count];

        if (rules.Id == "generic")
            return PlayerScores.Select(ps => ps.Tricks).ToArray();

        if (Type == RoundType.Durch)
        {
            scores[Bidder] = Won ? rules.DurchPoints : -rules.DurchPoints;
            return scores;
        }

        if (Type == RoundType.Bettel)
        {
            scores[Bidder] = Won ? rules.BettelPoints : -rules.BettelPoints;
            return scores;
        }

        for (int i = 0; i < PlayerScores.Count; i++)
        {
            var ps = PlayerScores[i];
            int total = ps.Meld + ps.Tricks;

            var bidderAbgegangen = PlayerScores.Count > Bidder && PlayerScores[Bidder].Abgegangen;

            if (i == Bidder)
            {
                if (ps.Abgegangen || total < Bid)
                    scores[i] = rules.DoubleMinus ? -(Bid * 2) : -Bid;
                else
                    scores[i] = total;
            }
            else
            {
                bool isPartner = rules.TeamMode && PlayerScores.Count == 4 && i == (Bidder + 2) % 4;
                if (ps.Abgegangen)
                    scores[i] = 0;
                else if (bidderAbgegangen && isPartner)
                    scores[i] = rules.DoubleMinus ? -(Bid * 2) : -Bid;
                else if (bidderAbgegangen)
                    scores[i] = total + PlayerScores.Count * rules.AbgegangenBonusPerPlayer;
                else
                    scores[i] = total;
            }
        }

        if (LastTrickWinner >= 0 && LastTrickWinner < scores.Length)
            scores[LastTrickWinner] += rules.LastTrickBonus;

        return scores;
    }
}

public class Game
{
    public string Einsatz { get; set; } = "";
    public long Id { get; set; }
    public long Date { get; set; }
    public long? SpielrundeId { get; set; }
    public List<string> Players { get; set; } = new();
    public RuleSet Rules { get; set; } = new();
    public List<Round> Rounds { get; set; } = new();
    public bool Finished { get; set; }
    /// <summary>Set for quick-entry games (no rounds). Index into Players.</summary>
    public int? QuickWinner { get; set; }

    public int[] GetPlayerTotals()
    {
        var totals = new int[Players.Count];
        foreach (var r in Rounds)
        {
            if (Rules.DurchSeparate && r.Type is RoundType.Durch or RoundType.Bettel)
                continue;
            var sc = r.CalcScores(Rules);
            for (int i = 0; i < totals.Length; i++) totals[i] += sc[i];
        }

        return totals;
    }

    public int[] GetDurchTotals()
    {
        var totals = new int[Players.Count];
        if (!Rules.DurchSeparate) return totals;
        foreach (var r in Rounds)
        {
            if (r.Type is not (RoundType.Durch or RoundType.Bettel)) continue;
            var sc = r.CalcScores(Rules);
            for (int i = 0; i < totals.Length; i++) totals[i] += sc[i];
        }

        return totals;
    }

    public int[] GetTeamTotals()
    {
        if (!Rules.TeamMode || Players.Count != 4) return new[] { 0, 0 };
        var t = new int[2];
        foreach (var r in Rounds)
        {
            var sc = r.CalcScores(Rules);
            t[0] += sc[0] + sc[2];
            t[1] += sc[1] + sc[3];
        }

        return t;
    }
}

public class AppState
{
    public List<Game> Games { get; set; } = new();
    public List<string> KnownPlayers { get; set; } = new();
    public List<Spielrunde> Spielrunden { get; set; } = new();
}

public class Spielrunde
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string GameType { get; set; } = GameTypeInfo.Binokel;
    public List<string> Players { get; set; } = new();
    /// <summary>SHA-256 Hex des Passworts, oder null = kein Passwort.</summary>
    public string? PasswordHash { get; set; }
    public int AssValue { get; set; } = 11;
    public int ZehnValue { get; set; } = 10;
    public int KoenigValue { get; set; } = 4;
    public int OberValue { get; set; } = 3;
    public int UnterValue { get; set; } = 2;
    public int LastTrickBonus { get; set; } = 10;
    public int AbgegangenBonusPerPlayer { get; set; } = 10;
}
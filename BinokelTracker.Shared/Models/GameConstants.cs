namespace BinokelTracker.Models;

public static class GameConstants
{
    public const int AbgegangenBonusPerPlayer = 10;
    public const int LastTrickBonus = 10;
}

public static class GameTypeInfo
{
    public const string Binokel    = "binokel";
    public const string Schafkopf  = "schafkopf";
    public const string Skat       = "skat";
    public const string Doppelkopf = "doppelkopf";
    public const string Romme      = "romme";
    public const string Andere     = "andere";

    public static readonly (string Id, string Icon, string Name, string Desc)[] All =
    {
        (Binokel,    "♠", "Binokel",      "Schwäbisches Kartenspiel"),
        (Schafkopf,  "♣", "Schafkopf",    "Bayerisches Kartenspiel"),
        (Skat,       "♦", "Skat",         "Deutsches Nationalkartenspiel"),
        (Doppelkopf, "♥", "Doppelkopf",   "Norddeutsches Kartenspiel"),
        (Romme,      "🎴", "Rommé",        "Klassisches Rummyspiel"),
        (Andere,     "⚙", "Andere",       "Eigenes Spiel tracken"),
    };

    public static string GetName(string id) =>
        All.FirstOrDefault(x => x.Id == id).Name ?? id;

    public static string GetIcon(string id) =>
        All.FirstOrDefault(x => x.Id == id).Icon ?? "?";

    public static bool IsBinokel(string id) => id == Binokel;
}


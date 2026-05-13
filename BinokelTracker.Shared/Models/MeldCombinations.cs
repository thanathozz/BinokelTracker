namespace BinokelTracker.Models;

public enum MeldType
{
    Binokel, Doppelbinokel,
    KonterTrumpf, KonterFarbe,
    FamilieTrumpf, FamilieFarbe,
    VierAsse, VierZehner, VierKoenige, VierOber, VierUnter
}

public record MeldCombination(MeldType Type, string Name, int Points);

public static class MeldCombinations
{
    public static readonly IReadOnlyList<MeldCombination> All = new[]
    {
        new MeldCombination(MeldType.Binokel,        "Binokel (♠O + ♦U)",    40),
        new MeldCombination(MeldType.Doppelbinokel,  "Doppelbinokel",        300),
        new MeldCombination(MeldType.KonterTrumpf,   "Konter in Trumpf",     150),
        new MeldCombination(MeldType.KonterFarbe,    "Konter in Farbe",      150),
        new MeldCombination(MeldType.FamilieTrumpf,  "Familie in Trumpf",     40),
        new MeldCombination(MeldType.FamilieFarbe,   "Familie in Farbe",      20),
        new MeldCombination(MeldType.VierAsse,       "Vier Asse",            100),
        new MeldCombination(MeldType.VierZehner,     "Vier Zehner",           80),
        new MeldCombination(MeldType.VierKoenige,    "Vier Könige",           60),
        new MeldCombination(MeldType.VierOber,       "Vier Ober",             40),
        new MeldCombination(MeldType.VierUnter,      "Vier Unter",            40),
    };
}

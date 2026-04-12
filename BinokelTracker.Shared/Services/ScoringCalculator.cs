using BinokelTracker.Models;

namespace BinokelTracker.Services;

public static class ScoringCalculator
{
    public static int[] CalcRoundScores(Round round, RuleSet rules)
    {
        var scores = new int[round.PlayerScores.Count];

        if (round.Type == RoundType.Durch)
        {
            scores[round.Bidder] = round.Won ? rules.DurchPoints : -rules.DurchPoints;
            return scores;
        }

        if (round.Type == RoundType.Bettel)
        {
            scores[round.Bidder] = round.Won ? rules.BettelPoints : -rules.BettelPoints;
            return scores;
        }

        bool bidderAbgegangen = round.PlayerScores.Count > round.Bidder
                                && round.PlayerScores[round.Bidder].Abgegangen;
        int abgBonus = round.PlayerScores.Count * rules.AbgegangenBonusPerPlayer;

        for (int i = 0; i < round.PlayerScores.Count; i++)
        {
            var ps = round.PlayerScores[i];
            int total = ps.Meld + ps.Tricks;

            if (i == round.Bidder)
            {
                scores[i] = (ps.Abgegangen || total < round.Bid)
                    ? (rules.DoubleMinus ? -(round.Bid * 2) : -round.Bid)
                    : total;
            }
            else
            {
                bool isPartner = rules.TeamMode && round.PlayerScores.Count == 4
                                 && i == (round.Bidder + 2) % 4;
                if (ps.Abgegangen)
                    scores[i] = 0;
                else if (bidderAbgegangen && isPartner)   // Team-Mitspieler → gleiche Strafe wie Reizer
                    scores[i] = rules.DoubleMinus ? -(round.Bid * 2) : -round.Bid;
                else if (bidderAbgegangen)
                    scores[i] = ps.Meld + abgBonus;       // Gegner → Meld + Bonus
                else if (ps.Tricks == 0 && ps.Meld > 0)
                    scores[i] = 0;                        // kein Stich → Meldung verfallen
                else
                    scores[i] = ps.Meld + ps.Tricks;
            }
        }

        if (round.LastTrickWinner >= 0 && round.LastTrickWinner < scores.Length)
            scores[round.LastTrickWinner] += rules.LastTrickBonus;

        return scores;
    }

    public static int[] GetPlayerTotals(Game game)
    {
        var totals = new int[game.Players.Count];
        foreach (var round in game.Rounds)
        {
            if (game.Rules.DurchSeparate && round.Type is RoundType.Durch or RoundType.Bettel)
                continue;
            var scores = CalcRoundScores(round, game.Rules);
            for (int i = 0; i < totals.Length; i++) totals[i] += scores[i];
        }
        return totals;
    }

    public static int[] GetDurchTotals(Game game)
    {
        var totals = new int[game.Players.Count];
        if (!game.Rules.DurchSeparate) return totals;
        foreach (var round in game.Rounds)
        {
            if (round.Type is not (RoundType.Durch or RoundType.Bettel)) continue;
            var scores = CalcRoundScores(round, game.Rules);
            for (int i = 0; i < totals.Length; i++) totals[i] += scores[i];
        }
        return totals;
    }

    public static int[] GetTeamTotals(Game game)
    {
        if (!game.Rules.TeamMode || game.Players.Count != 4) return new[] { 0, 0 };
        var t = new int[2];
        foreach (var round in game.Rounds)
        {
            var scores = CalcRoundScores(round, game.Rules);
            t[0] += scores[0] + scores[2];
            t[1] += scores[1] + scores[3];
        }
        return t;
    }

    /// <summary>
    /// Calculates a detailed per-player score breakdown for the round preview UI.
    /// Only applies to Normal rounds (not Durch/Bettel).
    /// </summary>
    public static ScoreBreakdown[] CalcNormalPreview(
        int bidder,
        int bidValue,
        bool[] abgegangen,
        int[] meld,
        int[] tricks,
        RuleSet rules,
        int lastTrickWinner = -1)
    {
        int playerCount = meld.Length;
        bool bidderAbgegangen = abgegangen.ElementAtOrDefault(bidder);
        int abgBonus = playerCount * rules.AbgegangenBonusPerPlayer;
        var result = new ScoreBreakdown[playerCount];

        for (int i = 0; i < playerCount; i++)
        {
            int mVal = meld.ElementAtOrDefault(i);
            int tVal = tricks.ElementAtOrDefault(i);
            bool pAbg = abgegangen.ElementAtOrDefault(i);
            bool isBidder = i == bidder;

            int finalScore;
            bool isLoss;
            string? lossReason;
            int bonus = 0;

            if (isBidder)
            {
                if (bidderAbgegangen)
                {
                    finalScore = rules.DoubleMinus ? -(bidValue * 2) : -bidValue;
                    isLoss = true;
                    lossReason = "Abgegangen";
                }
                else if (mVal + tVal < bidValue)
                {
                    finalScore = rules.DoubleMinus ? -(bidValue * 2) : -bidValue;
                    isLoss = true;
                    lossReason = rules.DoubleMinus
                        ? "Reizwert nicht erreicht → doppelt Minus"
                        : "Reizwert nicht erreicht";
                }
                else
                {
                    finalScore = mVal + tVal;
                    isLoss = false;
                    lossReason = null;
                }
            }
            else
            {
                bool isPartner = rules.TeamMode && playerCount == 4 && i == (bidder + 2) % 4;
                if (bidderAbgegangen && isPartner)
                {
                    finalScore = rules.DoubleMinus ? -(bidValue * 2) : -bidValue;
                    isLoss = true;
                    lossReason = "Reizer abgegangen";
                    mVal = 0;
                    tVal = 0;
                }
                else if (bidderAbgegangen)
                {
                    bonus = abgBonus;
                    finalScore = mVal + bonus;
                    isLoss = false;
                    lossReason = null;
                }
                else if (tVal == 0 && mVal > 0)
                {
                    finalScore = 0;
                    isLoss = false;
                    lossReason = "Kein Stich — Meldung verfallen";
                }
                else
                {
                    finalScore = mVal + tVal;
                    isLoss = false;
                    lossReason = null;
                }
            }

            int letzterStichBonus = (!isLoss && lastTrickWinner == i) ? rules.LastTrickBonus : 0;
            if (letzterStichBonus > 0) finalScore += letzterStichBonus;
            result[i] = new ScoreBreakdown(finalScore, isLoss, lossReason, mVal, tVal, pAbg, bonus, letzterStichBonus);
        }

        return result;
    }
}

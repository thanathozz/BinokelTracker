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
        bool gameWasPlayed = round.PlayerScores.Any(ps => ps.Tricks > 0);
        bool awardAbgBonus = bidderAbgegangen && !gameWasPlayed;
        int abgBonus = round.PlayerScores.Count * rules.AbgegangenBonusPerPlayer;

        int teamTotal = 0;
        if (rules.TeamMode && round.PlayerScores.Count == 4)
        {
            int partnerIndex = round.Bidder ^ 1;
            var bidderPs = round.PlayerScores[round.Bidder];
            var partnerPs = round.PlayerScores[partnerIndex];
            int bMeld = (!bidderPs.Abgegangen && bidderPs.Meld > 0 && bidderPs.Tricks == 0) ? 0 : bidderPs.Meld;
            int pMeld = (!partnerPs.Abgegangen && partnerPs.Meld > 0 && partnerPs.Tricks == 0) ? 0 : partnerPs.Meld;
            teamTotal = bMeld + bidderPs.Tricks + pMeld + partnerPs.Tricks + 
                        (round.LastTrickWinner == round.Bidder || round.LastTrickWinner == partnerIndex ? rules.LastTrickBonus : 0);
        }

        for (int i = 0; i < round.PlayerScores.Count; i++)
        {
            var ps = round.PlayerScores[i];
            int effectiveMeld = (!ps.Abgegangen && !bidderAbgegangen && ps.Meld > 0 && ps.Tricks == 0) ? 0 : ps.Meld;
            int total = effectiveMeld + ps.Tricks;

            bool isPartner = rules.TeamMode && round.PlayerScores.Count == 4
                             && i == (round.Bidder ^ 1);
            bool isBidder = (i == round.Bidder);

            if (isBidder)
            {
                bool won = (rules.TeamMode && round.PlayerScores.Count == 4)
                    ? teamTotal >= round.Bid
                    : (total + (round.LastTrickWinner == i ? rules.LastTrickBonus : 0)) >= round.Bid;

                scores[i] = (ps.Abgegangen || !won)
                    ? (rules.DoubleMinus ? -(round.Bid * 2) : -round.Bid)
                    : total;
            }
            else
            {
                if (ps.Abgegangen)
                    scores[i] = 0;
                else if (isPartner)
                {
                    // Im Team-Modus: Wenn das Team verliert (oder Reizer abgegangen), 
                    // erhält der Partner 0 Punkte. Die gesamte Team-Strafe liegt beim Reizer.
                    bool teamWon = (rules.TeamMode && round.PlayerScores.Count == 4)
                        ? teamTotal >= round.Bid
                        : true;

                    if (bidderAbgegangen || !teamWon)
                        scores[i] = 0;
                    else
                        scores[i] = total;
                }
                else if (awardAbgBonus)
                    scores[i] = effectiveMeld + abgBonus;
                else if (bidderAbgegangen)
                    scores[i] = total;
                else
                    scores[i] = total;
            }
        }

        if (round.LastTrickWinner >= 0 && round.LastTrickWinner < scores.Length
            && scores[round.LastTrickWinner] >= 0)
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
            t[0] += scores[0] + scores[1];
            t[1] += scores[2] + scores[3];
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
        int totalTricks = tricks.Sum();
        bool awardAbgBonus = bidderAbgegangen && totalTricks == 0;
        int abgBonus = playerCount * rules.AbgegangenBonusPerPlayer;

        int teamTotal = 0;
        if (rules.TeamMode && playerCount == 4)
        {
            int partnerIndex = bidder ^ 1;
            int bMeld = (!abgegangen.ElementAtOrDefault(bidder) && meld.ElementAtOrDefault(bidder) > 0 && tricks.ElementAtOrDefault(bidder) == 0) ? 0 : meld.ElementAtOrDefault(bidder);
            int pMeld = (!abgegangen.ElementAtOrDefault(partnerIndex) && meld.ElementAtOrDefault(partnerIndex) > 0 && tricks.ElementAtOrDefault(partnerIndex) == 0) ? 0 : meld.ElementAtOrDefault(partnerIndex);
            teamTotal = bMeld + tricks.ElementAtOrDefault(bidder) + pMeld + tricks.ElementAtOrDefault(partnerIndex) + 
                        (lastTrickWinner == bidder || lastTrickWinner == partnerIndex ? rules.LastTrickBonus : 0);
        }
        var result = new ScoreBreakdown[playerCount];

        for (int i = 0; i < playerCount; i++)
        {
            int mVal = meld.ElementAtOrDefault(i);
            int tVal = tricks.ElementAtOrDefault(i);
            bool pAbg = abgegangen.ElementAtOrDefault(i);
            bool isBidder = i == bidder;
            int effectiveMeld = (!pAbg && !bidderAbgegangen && mVal > 0 && tVal == 0) ? 0 : mVal;

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
                else if ((rules.TeamMode && playerCount == 4 ? teamTotal : effectiveMeld + tVal + (lastTrickWinner == i ? rules.LastTrickBonus : 0)) < bidValue)
                {
                    finalScore = rules.DoubleMinus ? -(bidValue * 2) : -bidValue;
                    isLoss = true;
                    lossReason = rules.DoubleMinus
                        ? "Reizwert nicht erreicht → doppelt Minus"
                        : "Reizwert nicht erreicht";
                }
                else
                {
                    finalScore = effectiveMeld + tVal;
                    isLoss = false;
                    lossReason = null;
                }
            }
            else
            {
                bool isPartner = rules.TeamMode && playerCount == 4 && i == (bidder ^ 1);
                if (bidderAbgegangen && isPartner)
                {
                    finalScore = rules.DoubleMinus ? -(bidValue * 2) : -bidValue;
                    isLoss = true;
                    lossReason = "Reizer abgegangen";
                    mVal = 0;
                    tVal = 0;
                }
                else if (awardAbgBonus)
                {
                    bonus = abgBonus;
                    finalScore = effectiveMeld + bonus;
                    isLoss = false;
                    lossReason = null;
                }
                else if (bidderAbgegangen) // Spiel gespielt → kein Bonus, normale Punkte
                {
                    finalScore = effectiveMeld + tVal;
                    isLoss = false;
                    lossReason = null;
                }
                else if (!pAbg && mVal > 0 && tVal == 0)
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

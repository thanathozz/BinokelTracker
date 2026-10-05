using BinokelTracker.Models;

namespace BinokelTracker.Services;

public static class ScoringCalculator
{
    public static int[] CalcRoundScores(Round round, RuleSet rules)
    {
        var strategy = ScoringStrategyFactory.GetStrategy(round, rules);
        return strategy.Calculate(round, rules);
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
        // Create a temporary round to leverage the strategy logic
        var round = new Round
        {
            Bidder = bidder,
            Bid = bidValue,
            LastTrickWinner = lastTrickWinner,
            PlayerScores = meld.Select((m, i) => new PlayerScore 
            { 
                Meld = m, 
                Tricks = tricks.ElementAtOrDefault(i), 
                Abgegangen = abgegangen.ElementAtOrDefault(i) 
            }).ToList()
        };

        var scores = CalcRoundScores(round, rules);
        var playerCount = meld.Length;
        var result = new ScoreBreakdown[playerCount];

        // Calculate some helper values for the breakdown
        bool bidderAbgegangen = abgegangen.ElementAtOrDefault(bidder);
        bool gameWasPlayed = tricks.Any(t => t > 0);
        bool awardAbgBonus = bidderAbgegangen && !gameWasPlayed;
        int abgBonus = playerCount * rules.AbgegangenBonusPerPlayer;

        for (int i = 0; i < playerCount; i++)
        {
            int mVal = meld.ElementAtOrDefault(i);
            int tVal = tricks.ElementAtOrDefault(i);
            bool pAbg = abgegangen.ElementAtOrDefault(i);
            bool isBidder = i == bidder;

            int finalScore = scores.Length > i ? scores[i] : 0;
            
            // Determine if it's a loss for this specific player
            bool isLoss = false;
            string? lossReason = null;
            if (isBidder)
            {
                if (pAbg) { isLoss = true; lossReason = "Abgegangen"; }
                else if (finalScore < 0) 
                { 
                    isLoss = true; 
                    lossReason = rules.DoubleMinus ? "Reizwert nicht erreicht → doppelt Minus" : "Reizwert nicht erreicht"; 
                }
            }
            else if (rules.TeamMode && playerCount == 4 && i == (bidder ^ 1))
            {
                if (finalScore < 0) { isLoss = true; lossReason = "Reizer abgegangen"; }
            }

            // Bonus extraction for the breakdown
            int bonus = 0;
            if (!isBidder && !pAbg && awardAbgBonus) bonus = abgBonus;

            int lastTrickBonus = 0;
            if (lastTrickWinner == i && finalScore >= 0)
            {
                // If the strategist added the bonus to finalScore, we reflect it here
                // For the bidder, the bonus is only kept if they won.
                if (isBidder)
                {
                    if (finalScore >= bidValue) lastTrickBonus = rules.LastTrickBonus;
                }
                else
                {
                    lastTrickBonus = rules.LastTrickBonus;
                }
            }

            result[i] = new ScoreBreakdown(finalScore, isLoss, lossReason, mVal, tVal, pAbg, bonus, lastTrickBonus);
        }

        return result;
    }
}

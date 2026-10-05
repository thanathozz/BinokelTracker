using BinokelTracker.Models;

namespace BinokelTracker.Services;

public class SpecialScoringStrategy : IScoringStrategy
{
    public int[] Calculate(Round round, RuleSet rules)
    {
        var scores = new int[round.PlayerScores.Count];

        if (round.Type == RoundType.Durch)
        {
            scores[round.Bidder] = round.Won ? rules.DurchPoints : -rules.DurchPoints;
        }
        else if (round.Type == RoundType.Bettel)
        {
            scores[round.Bidder] = round.Won ? rules.BettelPoints : -rules.BettelPoints;
        }

        return scores;
    }
}

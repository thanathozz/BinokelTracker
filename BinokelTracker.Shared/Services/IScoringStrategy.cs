using BinokelTracker.Models;

namespace BinokelTracker.Services;

public interface IScoringStrategy
{
    /// <summary>
    /// Calculates the scores for all players in the round.
    /// </summary>
    int[] Calculate(Round round, RuleSet rules);
}

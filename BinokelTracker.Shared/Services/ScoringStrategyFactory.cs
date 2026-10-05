using BinokelTracker.Models;

namespace BinokelTracker.Services;

public static class ScoringStrategyFactory
{
    public static IScoringStrategy GetStrategy(Round round, RuleSet rules)
    {
        IScoringStrategy strategy;

        if (round.Type == RoundType.Durch || round.Type == RoundType.Bettel)
        {
            strategy = new SpecialScoringStrategy();
        }
        else
        {
            strategy = new NormalScoringStrategy();
        }

        if (rules.TeamMode && round.PlayerScores.Count == 4)
        {
            strategy = new TeamScoringDecorator(strategy);
        }

        return strategy;
    }
}

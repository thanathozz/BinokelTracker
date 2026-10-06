using BinokelTracker.Models;
using System.Linq;

namespace BinokelTracker.Services;

public class TeamScoringDecorator : IScoringStrategy
{
    private readonly IScoringStrategy _innerStrategy;

    public TeamScoringDecorator(IScoringStrategy innerStrategy)
    {
        _innerStrategy = innerStrategy;
    }

    public int[] Calculate(Round round, RuleSet rules)
    {
        // 1. Basis-Berechnung durchführen (Solo-Logik)
        var scores = _innerStrategy.Calculate(round, rules);

        if (round.PlayerScores.Count != 4) return scores;

        int bidder = round.Bidder;
        int partner = bidder ^ 1;

        // 2. Team-Gewinnprüfung
        // Meld + Stiche beider Teammitglieder
        int teamTotal = 0;
        for (int i = 0; i < round.PlayerScores.Count; i++)
        {
            if (i == bidder || i == partner)
            {
                var ps = round.PlayerScores[i];
                int effectiveMeld = (!ps.Abgegangen && ps.Meld > 0 && ps.Tricks == 0) ? 0 : ps.Meld;
                teamTotal += effectiveMeld + ps.Tricks;
            }
        }

        // LastTrickBonus hilft auch dem Team, den Reizwert zu erreichen
        int teamTotalWithBonus = teamTotal;
        if (round.LastTrickWinner == bidder || round.LastTrickWinner == partner)
        {
            teamTotalWithBonus += rules.LastTrickBonus;
        }

        bool teamWon = teamTotalWithBonus >= round.Bid;
        bool bidderAbgegangen = round.PlayerScores.Count > bidder && round.PlayerScores[bidder].Abgegangen;

        // 3. Team-Strafe / Ergebnis anpassen
        if (!teamWon || bidderAbgegangen)
        {
            // Team-Strafe: Nur Reizer bekommt den vollen Minus-Wert, Partner bekommt 0.
            // Der LastTrickBonus mindert die Strafe NICHT.
            int penalty = rules.DoubleMinus ? -(round.Bid * 2) : -round.Bid;

            scores[bidder] = penalty;
            scores[partner] = 0;
        }
        else
        {
            // Team hat gewonnen: Jeder behält seine Punkte (bereits durch inner strategy berechnet).
            if (scores[bidder] < 0)
            {
                var ps = round.PlayerScores[bidder];
                int effectiveMeld = (!ps.Abgegangen && ps.Meld > 0 && ps.Tricks == 0) ? 0 : ps.Meld;
                scores[bidder] = effectiveMeld + ps.Tricks;
            }
        }

        return scores;
    }
}

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
                // In Team-Modus verfällt Meldung nur, wenn das gesamte Team keinen Stich macht?
                // Nein, wir bleiben bei der Regel: Meld verfällt, wenn der Einzelspieler keinen Stich macht.
                // Aber für die Team-Summe zählen wir alles.
                int effectiveMeld = (!ps.Abgegangen && ps.Meld > 0 && ps.Tricks == 0) ? 0 : ps.Meld;
                teamTotal += effectiveMeld + ps.Tricks;
            }
        }
        
        // Letzter Stich Bonus für das Team
        if (round.LastTrickWinner == bidder || round.LastTrickWinner == partner)
        {
            teamTotal += rules.LastTrickBonus;
        }

        bool teamWon = teamTotal >= round.Bid;
        bool bidderAbgegangen = round.PlayerScores.Count > bidder && round.PlayerScores[bidder].Abgegangen;

        // 3. Team-Strafe / Ergebnis anpassen
        if (!teamWon || bidderAbgegangen)
        {
            // Team-Strafe: Nur Reizer bekommt den vollen Minus-Wert, Partner bekommt 0.
            // Damit ist die Summe (scores[bidder] + scores[partner]) genau die Team-Strafe.
            int penalty = rules.DoubleMinus ? -(round.Bid * 2) : -round.Bid;
            
            scores[bidder] = penalty;
            scores[partner] = 0;
        }
        else
        {
            // Team hat gewonnen: Jeder behält seine Punkte (bereits durch inner strategy berechnet).
            // Wir müssen nur sicherstellen, dass der Reizer nicht fälschlicherweise 
            // eine Solo-Strafe erhalten hat, weil er allein den Reizwert nicht erreichte.
            
            // Die NormalScoringStrategy hat den Reizer bestraft, wenn (hisTotal < bid).
            // Im Team-Modus ist das okay, solange das TEAM gewonnen hat.
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

using BinokelTracker.Models;
using System.Linq;

namespace BinokelTracker.Services;

public class NormalScoringStrategy : IScoringStrategy
{
    public int[] Calculate(Round round, RuleSet rules)
    {
        var scores = new int[round.PlayerScores.Count];

        bool bidderAbgegangen = round.PlayerScores.Count > round.Bidder 
                                && round.PlayerScores[round.Bidder].Abgegangen;
        bool gameWasPlayed = round.PlayerScores.Any(ps => ps.Tricks > 0);
        bool awardAbgBonus = bidderAbgegangen && !gameWasPlayed;
        int abgBonus = round.PlayerScores.Count * rules.AbgegangenBonusPerPlayer;

        for (int i = 0; i < round.PlayerScores.Count; i++)
        {
            var ps = round.PlayerScores[i];
            
            // Meldung verfällt, wenn der Spieler keinen Stich macht,
            // ABER NICHT, wenn der Reizer abgegangen ist.
            int effectiveMeld = (ps.Tricks == 0 && !bidderAbgegangen && ps.Meld > 0) ? 0 : ps.Meld;
            int total = effectiveMeld + ps.Tricks;

            if (i == round.Bidder)
            {
                int totalWithBonus = total + (round.LastTrickWinner == i ? rules.LastTrickBonus : 0);
                
                if (ps.Abgegangen || totalWithBonus < round.Bid)
                    scores[i] = rules.DoubleMinus ? -(round.Bid * 2) : -round.Bid;
                else
                    scores[i] = total;
            }
            else
            {
                if (ps.Abgegangen)
                {
                    scores[i] = 0;
                }
                else
                {
                    // Bonus bei Reizer-Abgang vor Spielbeginn
                    scores[i] = awardAbgBonus ? total + abgBonus : total;
                }
            }
        }

        // Letzter Stich Bonus für Nicht-Reizer (oder gewinnenden Reizer)
        if (round.LastTrickWinner >= 0 && round.LastTrickWinner < scores.Length
            && scores[round.LastTrickWinner] >= 0)
        {
            scores[round.LastTrickWinner] += rules.LastTrickBonus;
        }

        return scores;
    }
}

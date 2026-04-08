namespace BinokelTracker.Models;

/// <summary>
/// Per-player score breakdown used for the round preview in AddRoundForm.
/// </summary>
public record ScoreBreakdown(
    int FinalScore,
    bool IsLoss,
    string? LossReason,
    int Meld,
    int Tricks,
    bool IsAbgegangen,
    int Bonus
);

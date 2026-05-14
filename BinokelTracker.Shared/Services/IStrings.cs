namespace BinokelTracker.Shared.Services;

public interface IStrings
{
    // Shell
    string Loading { get; }
    string SaveFailed { get; }
    string TabGames { get; }
    string TabStats { get; }
    string TabProfile { get; }

    // Common actions
    string Save { get; }
    string Cancel { get; }
    string Continue { get; }
    string Back { get; }
    string Finish { get; }
    string Delete { get; }
    string Switch { get; }
    string Logout { get; }
    string Yes { get; }
    string No { get; }
    string Open { get; }
    string Send { get; }
    string Sending { get; }
    string Reset { get; }

    // Game list
    string NoGames { get; }
    string NoGamesHint { get; }
    string NewGame { get; }
    string QuickChip { get; }
    string FinishedChip { get; }
    string WinnerLabel { get; }
    string RoundsAbbr { get; }

    // Spielrunde list
    string NoSpielrunde { get; }
    string NewSpielrunde { get; }
    string PrivateChip { get; }
    string ActiveLabel { get; }
    string GamesLabel { get; }
    string EnterPassword { get; }
    string PasswordProtected { get; }
    string WrongPassword { get; }

    // Game / player form
    string KnownPlayers { get; }
    string Players { get; }
    string AddPlayer { get; }
    string RemovePlayerBtn { get; }
    string TargetScore { get; }
    string Stakes { get; }
    string CreateGame { get; }
    string StartGame { get; }

    // BinokelGameSetup
    string RemovePlayerTitle { get; }
    string RemovePlayerBody { get; }
    string Rules { get; }
    string PlayerCount { get; }
    string TeamModeLabel { get; }
    string TargetPoints { get; }
    string DoubleMinus { get; }
    string AllowDurch { get; }
    string AllowBettel { get; }
    string AllowAbgehen { get; }
    string DurchSeparate { get; }
    string TeamHint { get; }
    string Teams { get; }
    string Goal { get; }

    // NewSpielrundeForm
    string NewSpielrundeTitle { get; }
    string Name { get; }
    string UsePassword { get; }
    string PasswordMismatch { get; }
    string StrichlisteMode { get; }
    string TrackingFields { get; }
    string CreateSpielrunde { get; }

    // QuickGameForm
    string QuickTitle { get; }
    string Date { get; }
    string StakesPerPlayer { get; }
    string NewPlayer { get; }
    string Participants { get; }
    string Winner { get; }
    string SaveEntry { get; }

    // GameDetail
    string Round { get; }
    string Rounds { get; }
    string NoRounds { get; }
    string AddRound { get; }
    string FinishGame { get; }
    string FinishGameBtn { get; }
    string TallyAdd { get; }
    string WinBonus { get; }
    string WinsWithPoints { get; }    // "{0} gewinnt mit {1} Punkten!"
    string WinsTeam { get; }          // "{0} & {1} gewinnen!"
    string WinsStrichliste { get; }   // "{0} gewinnt mit {1} Strichen!"

    // AddRoundForm
    string NewRoundTitle { get; }
    string EditRoundTitle { get; }

    // BinokelAddRound
    string Overview { get; }
    string StepByStep { get; }
    string StepPlayer { get; }
    string StepBidValue { get; }
    string StepMeld { get; }
    string StepTricks { get; }
    string StepLastTrick { get; }
    string StepResult { get; }
    string Bidder { get; }
    string GameTypeLabel { get; }
    string TypeNormal { get; }
    string BidValueLabel { get; }
    string MeldLabel { get; }
    string TricksLabel { get; }
    string TrumpLabel { get; }
    string PointsPerPlayer { get; }
    string TricksSum { get; }
    string TricksMustSum { get; }     // "Alle Stiche müssen zusammen {0} Punkte ergeben."
    string LastTrickLabel { get; }
    string FoldedAbbr { get; }
    string TotalLabel { get; }
    string ReportedLabel { get; }
    string NoMeld { get; }
    string NoMeldFolded { get; }
    string Done { get; }
    string NotDone { get; }
    string DurchMade { get; }
    string BettelMade { get; }
    string BidderFolded { get; }
    string BidderFoldedBonus { get; }
    string NoTrickMeld { get; }
    string Folded { get; }
    string YesFolded { get; }
    string NoStillPlaying { get; }
    string DoubleSuffix { get; }
    string OpenCalculator { get; }
    string LastTrickPoints { get; }   // "+{0} Punkte für {1}"

    // BinokelRoundCard
    string RoundPrefix { get; }
    string TagNormal { get; }
    string TagDurch { get; }
    string TagBettel { get; }
    string TagWon { get; }
    string TagLost { get; }
    string TagFolded { get; }
    string PlaysDurch { get; }
    string PlaysBettel { get; }
    string Bids { get; }
    string Points { get; }
    string NotFulfilled { get; }
    string WonWord { get; }
    string LostWord { get; }
    string FoldedWord { get; }
    string SpecialRoundNote { get; }
    string BidderFoldedNote { get; }
    string LastTrickNote { get; }     // "★ Letzter Stich · +{0} für {1}"

    // Statistics
    string StatsTitle { get; }
    string StatsGames { get; }
    string StatsRounds { get; }
    string StatsPlayers { get; }
    string StatsFinished { get; }
    string Rankings { get; }
    string Charts { get; }
    string NoData { get; }
    string NoDataHint { get; }
    string BidRate { get; }
    string GamesPlayedAbbr { get; }
    string ChartMoneyTitle { get; }
    string ChartWinsTitle { get; }
    string ChartWinRateTitle { get; }
    string ChartAvgBidTitle { get; }
    string ChartFoldedTitle { get; }
    string ChartOverbidTitle { get; }
    string ChartDurchTitle { get; }

    // BinokelSpielrundeSettings / card values
    string CardValues { get; }
    string Bonuses { get; }
    string LastTrickBonus { get; }
    string FoldedBonusPerPlayer { get; }
    string CardAss { get; }
    string CardZehn { get; }
    string CardKoenig { get; }
    string CardOber { get; }
    string CardUnter { get; }
    string CardSieben { get; }

    // AugenCalculator
    string CalculatorTitle { get; }
    string TotalPoints { get; }
    string AssignTo { get; }
    string LastTrick { get; }

    // Profile
    string ProfileTitle { get; }
    string Account { get; }
    string Email { get; }
    string DisplayName { get; }
    string Appearance { get; }
    string DesignTheme { get; }
    string LanguageLabel { get; }

    // Login
    string SignIn { get; }
    string Register { get; }
    string WhatIsYourName { get; }
    string DisplayNameHint { get; }
    string MissingCredentials { get; }
    string MissingName { get; }

    // GameTypeSelect
    string ChooseGame { get; }
    string BadgeFull { get; }
    string BadgeSimple { get; }

    // DeleteRound confirm
    string DeleteRoundTitle { get; }
    string DeleteRoundBody { get; }

    // FeedbackDialog
    string ReportBug { get; }
    string NoScreenshot { get; }
    string BugDescriptionPlaceholder { get; }

    // RulesDialog
    string RulesTitle { get; }
    string Melds { get; }
    string SpecialMelds { get; }
    string PointValues { get; }
}

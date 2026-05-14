namespace BinokelTracker.Shared.Services;

public class StringsEn : IStrings
{
    public string Loading           => "Loading...";
    public string SaveFailed        => "Save failed";
    public string TabGames          => "Games";
    public string TabStats          => "Stats";
    public string TabProfile        => "Profile";

    public string Save              => "Save";
    public string Cancel            => "Cancel";
    public string Continue          => "Continue";
    public string Back              => "Back";
    public string Finish            => "Finish";
    public string Delete            => "Delete";
    public string Switch            => "Switch";
    public string Logout            => "Log out";
    public string Yes               => "Yes";
    public string No                => "No";
    public string Open              => "Open";
    public string Send              => "Send";
    public string Sending           => "Sending…";
    public string Reset             => "Reset";

    public string NoGames           => "No games yet";
    public string NoGamesHint       => "Start your first game!";
    public string NewGame           => "New Game";
    public string QuickChip         => "Quick";
    public string FinishedChip      => "Finished";
    public string WinnerLabel       => "Winner";
    public string RoundsAbbr        => "Rds.";

    public string NoSpielrunde      => "No sessions yet";
    public string NewSpielrunde     => "New Session";
    public string PrivateChip       => "Private";
    public string ActiveLabel       => "active";
    public string GamesLabel        => "game(s)";
    public string EnterPassword     => "Enter password";
    public string PasswordProtected => "is password protected";
    public string WrongPassword     => "Wrong password.";

    public string KnownPlayers      => "Known players";
    public string Players           => "Players";
    public string AddPlayer         => "+ Player";
    public string RemovePlayerBtn   => "− Remove";
    public string TargetScore       => "Target score";
    public string Stakes            => "Stakes (optional)";
    public string CreateGame        => "Create game";
    public string StartGame         => "Start game";

    public string RemovePlayerTitle => "Remove player?";
    public string RemovePlayerBody  => "will be removed from the player list.";
    public string Rules             => "Rules";
    public string PlayerCount       => "Number of players";
    public string TeamModeLabel     => "Team mode (cross)";
    public string TargetPoints      => "Target points";
    public string DoubleMinus       => "Double minus on overbid";
    public string AllowDurch        => "Allow Durch";
    public string AllowBettel       => "Allow Bettel";
    public string AllowAbgehen      => "Allow Abgehen";
    public string DurchSeparate     => "Durch points separate";
    public string TeamHint          => "Players 1 & 3 = Team 1 · Players 2 & 4 = Team 2 (cross)";
    public string Teams             => "Teams (cross)";
    public string Goal              => "Goal:";
    public string Missing           => "to go";

    public string NewSpielrundeTitle => "New session";
    public string Name              => "Name";
    public string UsePassword       => "Password protect";
    public string PasswordMismatch  => "Passwords do not match.";
    public string StrichlisteMode   => "Tally mode";
    public string TrackingFields    => "Tracking fields";
    public string CreateSpielrunde  => "Create session";

    public string QuickTitle        => "Quick entry";
    public string Date              => "Date";
    public string StakesPerPlayer   => "Stakes per player (€)";
    public string NewPlayer         => "New player";
    public string Participants      => "Players";
    public string Winner            => "Winner";
    public string SaveEntry         => "Save entry";

    public string Round             => "Round";
    public string Rounds            => "Rounds";
    public string NoRounds          => "No rounds played yet";
    public string AddRound          => "+ Round";
    public string FinishGame        => "Finish";
    public string FinishGameBtn     => "End game";
    public string TallyAdd          => "+ Tally";
    public string WinBonus          => "Win:";
    public string WinsWithPoints    => "🏆 {0} wins with {1} points!";
    public string WinsTeam          => "🏆 {0} & {1} win!";
    public string WinsStrichliste   => "🏆 {0} wins with {1} tallies!";

    public string NewRoundTitle     => "New round";
    public string EditRoundTitle    => "Edit round";

    public string Overview          => "Overview";
    public string StepByStep        => "Step by step";
    public string StepPlayer        => "Player";
    public string StepBidValue      => "Bid value";
    public string StepMeld          => "Meld";
    public string StepTricks        => "Tricks";
    public string StepLastTrick     => "Last trick";
    public string StepResult        => "Result";
    public string Bidder            => "Bidder";
    public string GameTypeLabel     => "Game type";
    public string TypeNormal        => "Normal";
    public string BidValueLabel     => "Bid value";
    public string MeldLabel         => "Meld";
    public string TricksLabel       => "Tricks";
    public string TrumpLabel        => "Trump (optional)";
    public string PointsPerPlayer   => "Points per player";
    public string TricksSum         => "Tricks total:";
    public string TricksMustSum     => "All tricks must sum to {0} points.";
    public string LastTrickLabel    => "Last trick";
    public string FoldedAbbr        => "Fld.";
    public string TotalLabel        => "Total";
    public string ReportedLabel     => "Meld";
    public string NoMeld            => "No meld";
    public string NoMeldFolded      => "No meld (folded)";
    public string Done              => "Made it";
    public string NotDone           => "Failed";
    public string DurchMade         => "Won all tricks?";
    public string BettelMade        => "Won no tricks?";
    public string BidderFolded      => "Bidder folded";
    public string BidderFoldedBonus => "Bonus (bidder folded)";
    public string NoTrickMeld       => "No trick — meld lost";
    public string Folded            => "Folded";
    public string YesFolded         => "Yes — folded";
    public string NoStillPlaying    => "No — still playing";
    public string DoubleSuffix      => "double";
    public string OpenCalculator    => "Eye calculator";
    public string LastTrickPoints   => "+{0} pts for {1}";

    public string RoundPrefix       => "ROUND";
    public string TagNormal         => "NORMAL";
    public string TagDurch          => "DURCH";
    public string TagBettel         => "BETTEL";
    public string TagWon            => "WON";
    public string TagLost           => "LOST";
    public string TagFolded         => "FOLDED";
    public string PlaysDurch        => "plays a";
    public string PlaysBettel       => "plays a";
    public string Bids              => "bids";
    public string Points            => "pts.";
    public string NotFulfilled      => "not fulfilled";
    public string WonWord           => "won";
    public string LostWord          => "lost";
    public string FoldedWord        => "folded";
    public string SpecialRoundNote  => "Special round · scored separately";
    public string BidderFoldedNote  => "Bidder folded · co-players +bonus";
    public string LastTrickNote     => "★ Last trick · +{0} for {1}";

    public string StatsTitle        => "Statistics";
    public string StatsGames        => "Games";
    public string StatsRounds       => "Rounds";
    public string StatsPlayers      => "Players";
    public string StatsFinished     => "Finished";
    public string Rankings          => "Rankings";
    public string Charts            => "Charts";
    public string NoData            => "No data yet";
    public string NoDataHint        => "Play a few rounds first!";
    public string BidRate           => "Bid rate";
    public string GamesPlayedAbbr   => "gm.";
    public string ChartMoneyTitle   => "Gain / Loss (€)";
    public string ChartWinsTitle    => "Wins";
    public string ChartWinRateTitle => "Bid rate";
    public string ChartAvgBidTitle  => "Avg. bid";
    public string ChartFoldedTitle  => "Folded";
    public string ChartOverbidTitle => "Overbid";
    public string ChartDurchTitle   => "Durch — Wins";

    public string CardValues        => "Card values (eyes)";
    public string Bonuses           => "Bonuses";
    public string LastTrickBonus    => "Last trick";
    public string FoldedBonusPerPlayer => "Fold bonus/pl.";
    public string CardAss           => "Ace";
    public string CardZehn          => "Ten";
    public string CardKoenig        => "King";
    public string CardOber          => "Queen";
    public string CardUnter         => "Jack";
    public string CardSieben        => "Seven";

    public string CalculatorTitle   => "Eye calculator";
    public string TotalPoints       => "Total points";
    public string AssignTo          => "Assign to:";
    public string LastTrick         => "Last trick";

    public string ProfileTitle      => "Profile ◉";
    public string Account           => "Account";
    public string Email             => "E-mail";
    public string DisplayName       => "Display name";
    public string Appearance        => "Appearance";
    public string DesignTheme       => "Design theme";
    public string LanguageLabel     => "Language";

    public string SignIn            => "Sign in";
    public string Register          => "Register";
    public string WhatIsYourName    => "What's your name?";
    public string DisplayNameHint   => "This name will be shown during play.";
    public string MissingCredentials => "Please enter e-mail and password.";
    public string MissingName       => "Please enter a name.";
    public string NickLabel         => "Nick";
    public string NickHint          => "Your unique username";
    public string NickChecking      => "Checking…";
    public string NickAvailable     => "Available";
    public string NickTaken         => "Nick already taken.";
    public string NickInvalid       => "3–20 chars, only a–z, 0–9 and _";

    public string ChooseGame        => "Choose game";
    public string BadgeFull         => "Full";
    public string BadgeSimple       => "Simple";

    public string DeleteRoundTitle   => "Delete round?";
    public string DeleteRoundBody    => "This round will be permanently deleted.";
    public string DeleteGameTitle    => "Delete game?";
    public string DeleteGameBody     => "This game and all its rounds will be permanently deleted.";

    public string ReportBug         => "Report bug";
    public string NoScreenshot      => "No screenshot available";
    public string BugDescriptionPlaceholder => "What happened? Describe the bug briefly...";

    public string RulesTitle        => "Binokel Rules";
    public string Melds             => "Melds";
    public string SpecialMelds      => "Special melds";
    public string PointValues       => "Point values";
}

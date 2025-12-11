namespace SubspaceStats.Areas.League.Models.Season
{
    public class OverviewViewModel : ISeasonViewModel
    {
        public required SeasonDetails SeasonDetails { get; init; }
        public SeasonPage Page => SeasonPage.Overview;
        public required LeagueSeasonChooserViewModel LeagueSeasonChooser { get; init; }
        public required List<ScheduledGame> ScheduledGames { get; init; }
        public required List<TeamStanding> Standings { get; init; }
        public required List<GameRecord> CompletedGames { get; init; }
    }
}

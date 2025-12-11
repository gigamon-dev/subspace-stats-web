using SubspaceStats.Models;

namespace SubspaceStats.Areas.League.Models.Season
{
    public class DetailsViewModel : ISeasonViewModel
    {
        public required SeasonDetails SeasonDetails { get; init; }
        public SeasonPage Page => SeasonPage.Details;
        public required LeagueSeasonChooserViewModel LeagueSeasonChooser { get; init; }
        public string? RefreshPlayerStatsMessage { get; set; }
        public string? RefreshTeamStatsMessage { get; set; }
    }
}

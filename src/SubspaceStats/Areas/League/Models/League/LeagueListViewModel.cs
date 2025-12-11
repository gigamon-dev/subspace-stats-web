using SubspaceStats.Models;

namespace SubspaceStats.Areas.League.Models.League
{
    public class LeagueListViewModel : ILeagueViewModel
    {
        public LeagueModel? League => null;
        public LeagueSection Section => LeagueSection.Manage;
        public required LeagueSeasonChooserViewModel LeagueSeasonChooser { get; init; }
        public required List<LeagueModel> Leagues { get; init; }
        public required OrderedDictionary<long, GameType> GameTypes;
    }
}

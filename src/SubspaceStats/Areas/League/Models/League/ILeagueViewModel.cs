namespace SubspaceStats.Areas.League.Models.League
{
    public interface ILeagueViewModel : ILeagueSeasonViewModel
    {
        public LeagueModel? League { get; }
        public LeagueSection Section { get; }
    }

    public enum LeagueSection
    {
        Details,
        Roles,
        Edit,
        Manage,
    }
}

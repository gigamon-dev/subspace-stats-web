namespace SubspaceStats.Areas.League.Models.Season
{
    public interface ISeasonViewModel : ILeagueSeasonViewModel
    {
        public SeasonDetails SeasonDetails { get; }
        public SeasonPage Page { get; }
    }

    public enum SeasonPage
    {
        Overview,
        Rosters,
        Details,
        Players,
        Teams,
        Games,
        Rounds,
        Roles,
    }
}

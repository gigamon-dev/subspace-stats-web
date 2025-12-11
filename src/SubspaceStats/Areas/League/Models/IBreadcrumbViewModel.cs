namespace SubspaceStats.Areas.League.Models
{
    public interface IBreadcrumbViewModel
    {
    }

    public interface ILeagueSeasonViewModel : IBreadcrumbViewModel
    {
        LeagueSeasonChooserViewModel LeagueSeasonChooser { get; }
    }
}

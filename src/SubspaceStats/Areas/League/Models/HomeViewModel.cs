namespace SubspaceStats.Areas.League.Models
{
    public class HomeViewModel : IBreadcrumbViewModel
    {
        public required List<SeasonStandings> Standings { get; init; }
    }
}

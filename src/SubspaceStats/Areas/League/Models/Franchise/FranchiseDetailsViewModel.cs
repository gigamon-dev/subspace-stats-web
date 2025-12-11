namespace SubspaceStats.Areas.League.Models.Franchise
{
    public class FranchiseDetailsViewModel : IFranchiseBreadcrumb
    {
        public required FranchiseModel Franchise { get; set; }
        public required List<TeamAndSeason> TeamsAndSeasons { get; set; }
    }
}

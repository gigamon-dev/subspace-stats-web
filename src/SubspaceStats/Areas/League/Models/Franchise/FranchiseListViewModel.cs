namespace SubspaceStats.Areas.League.Models.Franchise
{
    public class FranchiseListViewModel : IFranchiseBreadcrumb
    {
        public required List<FranchiseListItem> FranchiseList { get; init; }
    }
}

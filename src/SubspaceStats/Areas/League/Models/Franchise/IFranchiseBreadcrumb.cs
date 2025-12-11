namespace SubspaceStats.Areas.League.Models.Franchise
{
    public interface IFranchiseBreadcrumb : IBreadcrumbViewModel
    {
        FranchiseModel? Franchise { get => null; }
    }
}

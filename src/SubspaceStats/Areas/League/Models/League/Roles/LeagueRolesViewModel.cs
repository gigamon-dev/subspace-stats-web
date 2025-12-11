namespace SubspaceStats.Areas.League.Models.League.Roles
{
    public class LeagueRolesViewModel : ILeagueViewModel
    {
        public required LeagueModel League { get; init; }
        public LeagueSection Section => LeagueSection.Roles;
        public required LeagueSeasonChooserViewModel LeagueSeasonChooser { get; init; }
        public required List<LeagueUserRole> Roles { get; init; }

        public AddUserRoleViewModel? AddUserRole { get; init; }
        public string? AddUserRoleMessage { get; init; }
    }
}

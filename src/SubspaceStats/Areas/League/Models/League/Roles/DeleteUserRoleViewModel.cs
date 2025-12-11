using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SubspaceStats.Areas.League.Authorization;
using System.ComponentModel.DataAnnotations;

namespace SubspaceStats.Areas.League.Models.League.Roles
{
    public class DeleteUserRoleViewModel : ILeagueViewModel
    {
        [Required]
        public string? UserId { get; set; }

        [Required]
        public LeagueRole? Role { get; set; }

        [BindNever]
        [ValidateNever]
        public string? UserName { get; set; }

        [BindNever]
        [ValidateNever]
        public required LeagueModel League { get; set; }

        public LeagueSection Section => LeagueSection.Roles;

        [BindNever]
        [ValidateNever]
        public required LeagueSeasonChooserViewModel LeagueSeasonChooser { get; init; }
    }
}

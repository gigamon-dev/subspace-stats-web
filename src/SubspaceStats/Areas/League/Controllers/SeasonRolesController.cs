using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SubspaceStats.Areas.Identity.Data;
using SubspaceStats.Areas.League.Authorization;
using SubspaceStats.Areas.League.Models;
using SubspaceStats.Areas.League.Models.League;
using SubspaceStats.Areas.League.Models.Season;
using SubspaceStats.Areas.League.Models.Season.Roles;
using SubspaceStats.Services;

namespace SubspaceStats.Areas.League.Controllers
{
    [Area("League")]
    public class SeasonRolesController(
        IAuthorizationService authorizationService,
        UserManager<SubspaceStatsUser> userManager,
        ILeagueRepository leagueRepository) : Controller
    {
        private readonly IAuthorizationService _authorizationService = authorizationService;
        private readonly UserManager<SubspaceStatsUser> _userManager = userManager;
        private readonly ILeagueRepository _leagueRepository = leagueRepository;

        // GET League/Season/{seasonId}/Roles
        public async Task<IActionResult> Index(long? seasonId, CancellationToken cancellationToken)
        {
            if (seasonId is null)
            {
                return NotFound();
            }

            SeasonDetails? seasonDetails = await _leagueRepository.GetSeasonDetailsAsync(seasonId.Value, cancellationToken);
            if (seasonDetails is null)
            {
                return NotFound();
            }

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(seasonDetails.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            // Season roles can be read by Season Managers, League Managers, and Administrators.
            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, seasonDetails, PolicyNames.Manager);
            if (!result.Succeeded)
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    return Forbid();
                }
                else
                {
                    return Challenge();
                }
            }

            List<LeagueNavItem> leaguesWithSeasons = await _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
            LeagueSeasonChooserViewModel seasonChooser = new(leaguesWithSeasons, null, seasonId.Value);
            LeagueNavItem? currentLeague = seasonChooser.SelectedLeague;
            SeasonNavItem? currentSeason = seasonChooser.SelectedSeason;
            if (currentLeague is null || currentSeason is null)
            {
                return NotFound();
            }

            return View(
                new SeasonRolesViewModel
                {
                    SeasonDetails = seasonDetails,
                    League = league,
                    LeagueSeasonChooser = seasonChooser,
                    Roles = await _leagueRepository.GetSeasonUserRoles(seasonId.Value, cancellationToken),
                    AddUserRole = new AddUserRoleViewModel()
                    {
                        Role = SeasonRole.Manager,
                    },
                    AddUserRoleMessage = TempData["AddUserRoleMessage"] as string,
                });
        }

        // POST League/Season/{seasonId}/Roles/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            long? seasonId,
            [Bind(Prefix = "AddUserRole")] AddUserRoleViewModel model,
            CancellationToken cancellationToken)
        {
            if (seasonId is null)
            {
                return NotFound();
            }

            SeasonDetails? seasonDetails = await _leagueRepository.GetSeasonDetailsAsync(seasonId.Value, cancellationToken);
            if (seasonDetails is null)
            {
                return NotFound();
            }

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(seasonDetails.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            // Season roles can be modified by League Managers and Administrators.
            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, league, PolicyNames.Manager);
            if (!result.Succeeded)
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    return Forbid();
                }
                else
                {
                    return Challenge();
                }
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.UserName) || model.Role is null)
            {
                TempData["AddUserRoleMessage"] = "Invalid input.";
                return RedirectToAction("Index");
            }

            SubspaceStatsUser? user = await _userManager.FindByNameAsync(model.UserName);
            if (user is null)
            {
                TempData["AddUserRoleMessage"] = $"User '{model.UserName}' not found.";
                return RedirectToAction("Index");
            }

            await _leagueRepository.InsertSeasonUserRole(seasonId.Value, user.Id, model.Role.Value, cancellationToken);
            TempData["AddUserRoleMessage"] = $"Successfully assigned '{model.Role}' to '{model.UserName}'.";
            return RedirectToAction("Index");
        }

        // GET League/Season/{seasonId}/Roles/Delete
        public async Task<IActionResult> Delete(
            long? seasonId,
            DeleteUserRoleViewModel model,
            CancellationToken cancellationToken)
        {
            if (seasonId is null)
            {
                return NotFound();
            }

            SeasonDetails? seasonDetails = await _leagueRepository.GetSeasonDetailsAsync(seasonId.Value, cancellationToken);
            if (seasonDetails is null)
            {
                return NotFound();
            }

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(seasonDetails.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            // Season roles can be modified by League Managers and Administrators.
            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, league, PolicyNames.Manager);
            if (!result.Succeeded)
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    return Forbid();
                }
                else
                {
                    return Challenge();
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            List<LeagueNavItem> leaguesWithSeasons = await _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
            LeagueSeasonChooserViewModel seasonChooser = new(leaguesWithSeasons, null, seasonId.Value);
            LeagueNavItem? currentLeague = seasonChooser.SelectedLeague;
            SeasonNavItem? currentSeason = seasonChooser.SelectedSeason;
            if (currentLeague is null || currentSeason is null)
            {
                return NotFound();
            }

            SubspaceStatsUser? user = await _userManager.FindByIdAsync(model.UserId!);
            if (user is not null)
            {
                model.UserName = user.UserName;
            }

            model.SeasonDetails = seasonDetails;
            model.LeagueSeasonChooser = seasonChooser;
            return View(model);
        }

        // POST League/Season/{seasonId}/Roles/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirm(
            long? seasonId,
            DeleteUserRoleViewModel model,
            CancellationToken cancellationToken)
        {
            if (seasonId is null)
            {
                return NotFound();
            }

            SeasonDetails? seasonDetails = await _leagueRepository.GetSeasonDetailsAsync(seasonId.Value, cancellationToken);
            if (seasonDetails is null)
            {
                return NotFound();
            }

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(seasonDetails.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            // Season roles can be modified by League Managers and Administrators.
            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, league, PolicyNames.Manager);
            if (!result.Succeeded)
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    return Forbid();
                }
                else
                {
                    return Challenge();
                }
            }

            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            SubspaceStatsUser? user = await _userManager.FindByIdAsync(model.UserId!);
            if (user is null)
            {
                return RedirectToAction("Index");
            }

            await _leagueRepository.DeleteSeasonUserRole(seasonId.Value, model.UserId!, model.Role!.Value, cancellationToken);
            return RedirectToAction("Index");
        }
    }
}

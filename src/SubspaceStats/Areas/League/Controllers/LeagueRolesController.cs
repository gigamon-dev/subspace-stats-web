using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SubspaceStats.Areas.Identity.Data;
using SubspaceStats.Areas.League.Authorization;
using SubspaceStats.Areas.League.Models;
using SubspaceStats.Areas.League.Models.League;
using SubspaceStats.Areas.League.Models.League.Roles;
using SubspaceStats.Services;

namespace SubspaceStats.Areas.League.Controllers
{
    [Area("league")]
    public class LeagueRolesController(
        IAuthorizationService authorizationService,
        UserManager<SubspaceStatsUser> userManager,
        ILeagueRepository leagueRepository) : Controller
    {
        private readonly IAuthorizationService _authorizationService = authorizationService;
        private readonly UserManager<SubspaceStatsUser> _userManager = userManager;
        private readonly ILeagueRepository _leagueRepository = leagueRepository;

        // GET League/{leagueId}/Roles
        public async Task<IActionResult> Index(long? leagueId, CancellationToken cancellationToken)
        {
            if (leagueId is null)
            {
                return NotFound();
            }

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(leagueId.Value, cancellationToken); ;
            if (league is null)
            {
                return NotFound();
            }

            // Managers can view roles, but only administrators can modify them.
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

            Task<List<LeagueNavItem>> navTask = _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
            Task<List<LeagueUserRole>> rolesTask = _leagueRepository.GetLeagueUserRoles(leagueId.Value, cancellationToken);

            await Task.WhenAll(navTask, rolesTask);

            return View(
                new LeagueRolesViewModel
                {
                    League = league,
                    LeagueSeasonChooser = new LeagueSeasonChooserViewModel(navTask.Result, league.Id, null),
                    Roles = rolesTask.Result,
                    AddUserRole = new AddUserRoleViewModel()
                    {
                        Role = LeagueRole.Manager,
                    },
                    AddUserRoleMessage = TempData["AddUserRoleMessage"] as string,
                });
        }

        // POST League/{leagueId}/Roles/Add
        [Authorize(Roles = RoleNames.Administrator)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            long? leagueId,
            [Bind(Prefix = "AddUserRole")] AddUserRoleViewModel model,
            CancellationToken cancellationToken)
        {
            if (leagueId is null)
            {
                return NotFound();
            }

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(leagueId.Value, cancellationToken); ;
            if (league is null)
            {
                return NotFound();
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

            await _leagueRepository.InsertLeagueUserRole(leagueId.Value, user.Id, model.Role.Value, cancellationToken);
            TempData["AddUserRoleMessage"] = $"Successfully assigned '{model.Role}' to '{model.UserName}'.";
            return RedirectToAction("Index");
        }

        // GET League/{leagueId}/Roles/Delete
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> Delete(
            long? leagueId,
            DeleteUserRoleViewModel model,
            CancellationToken cancellationToken)
        {
            if (leagueId is null)
            {
                return NotFound();
            }

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(leagueId.Value, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            SubspaceStatsUser? user = await _userManager.FindByIdAsync(model.UserId!);
            if (user is not null)
            {
                model.UserName = user.UserName;
            }

            model.League = league;
            return View(model);
        }

        // POST League/{leagueId}/Roles/Delete
        [Authorize(Roles = RoleNames.Administrator)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirm(
            long? leagueId,
            DeleteUserRoleViewModel model,
            CancellationToken cancellationToken)
        {
            if (leagueId is null)
            {
                return NotFound();
            }

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(leagueId.Value, cancellationToken); ;
            if (league is null)
            {
                return NotFound();
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

            await _leagueRepository.DeleteLeagueUserRole(leagueId.Value, model.UserId!, model.Role!.Value, cancellationToken);
            return RedirectToAction("Index");
        }
    }
}

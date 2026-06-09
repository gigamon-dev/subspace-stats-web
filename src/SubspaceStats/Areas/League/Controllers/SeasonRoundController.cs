using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubspaceStats.Areas.League.Authorization;
using SubspaceStats.Areas.League.Models.League;
using SubspaceStats.Areas.League.Models.Season;
using SubspaceStats.Areas.League.Models.Season.Round;
using SubspaceStats.Services;

namespace SubspaceStats.Areas.League.Controllers
{
    [Area("League")]
    public class SeasonRoundController(
        IAuthorizationService authorizationService, 
        ILeagueRepository leagueRepository) : Controller
    {
        private readonly IAuthorizationService _authorizationService = authorizationService;
        private readonly ILeagueRepository _leagueRepository = leagueRepository;

        // GET League/Season/{seasonId}/Round/Create
        public async Task<IActionResult> Create(long seasonId, CancellationToken cancellationToken)
        {
            SeasonModel? season = await _leagueRepository.GetSeasonAsync(seasonId, cancellationToken);
            if (season is null)
            {
                return NotFound();
            }

            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, season, PolicyNames.Manager);
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

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(season.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            var rounds = await _leagueRepository.GetSeasonRoundsAsync(seasonId, cancellationToken);
            int roundNumber = 1;
            foreach(int round in rounds.Keys)
            {
                if (round >= roundNumber)
                    roundNumber = round + 1;
            }

            return View(
                new SeasonRoundViewModel
                {
                    Round = new SeasonRound
                    {
                        SeasonId = seasonId,
                        RoundNumber = roundNumber,
                        RoundName = "", // dummy
                    },
                    Season = season,
                    League = league,
                });
        }

        // POST League/Season/{seasonId}/Round/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            long seasonId,
            [Bind("SeasonId", "RoundNumber", "RoundName", "RoundDescription")] SeasonRound round,
            CancellationToken cancellationToken)
        {
            if (seasonId != round.SeasonId)
            {
                return NotFound();
            }

            SeasonModel? season = await _leagueRepository.GetSeasonAsync(seasonId, cancellationToken);
            if (season is null)
            {
                return NotFound();
            }

            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, season, PolicyNames.Manager);
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

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(season.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(
                    new SeasonRoundViewModel
                    {
                        Round = round,
                        Season = season,
                        League = league,
                    });
            }

            await _leagueRepository.InsertSeasonRoundAsync(round, cancellationToken);
            return RedirectToAction("Rounds", "Season", new { seasonId });
        }

        // This is called by the validation logic that the [Remote] attribute on SeasonRound.RoundNumber adds.
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> ValidateSeasonRound(long seasonId, int roundNumber, CancellationToken cancellationToken)
        {
            if (await _leagueRepository.GetSeasonRoundAsync(seasonId, roundNumber, cancellationToken) is not null)
            {
                return Json($"Round {roundNumber} already exists.");
            }

            return Json(true);
        }

        // GET League/Season/{seasonId}/Round/{roundNumber}/Edit
        public async Task<ActionResult> Edit(long seasonId, int roundNumber, CancellationToken cancellationToken)
        {
            SeasonModel? season = await _leagueRepository.GetSeasonAsync(seasonId, cancellationToken);
            if (season is null)
            {
                return NotFound();
            }

            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, season, PolicyNames.Manager);
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

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(season.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            SeasonRound? round = await _leagueRepository.GetSeasonRoundAsync(seasonId, roundNumber, cancellationToken);
            if (round is null)
            {
                return NotFound();
            }

            return View(
                new SeasonRoundViewModel
                {
                    Round = round,
                    Season = season,
                    League = league,
                });
        }

        // POST League/Season/{seasonId}/Round/{roundNumber}/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            long seasonId,
            int roundNumber,
            SeasonRound round,
            CancellationToken cancellationToken)
        {
            if (seasonId != round.SeasonId || roundNumber != round.RoundNumber)
            {
                return NotFound();
            }

            SeasonModel? season = await _leagueRepository.GetSeasonAsync(seasonId, cancellationToken);
            if (season is null)
            {
                return NotFound();
            }

            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, season, PolicyNames.Manager);
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

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(season.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            SeasonRound? currentRound = await _leagueRepository.GetSeasonRoundAsync(seasonId, roundNumber, cancellationToken);
            if (currentRound is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(
                    new SeasonRoundViewModel
                    {
                        Round = round,
                        Season = season,
                        League = league,
                    });
            }

            await _leagueRepository.UpdateSeasonRoundAsync(round, cancellationToken);
            return RedirectToAction("Rounds", "Season", new { seasonId });
        }

        // GET League/Season/{seasonId}/Round/{roundNumber}/Delete
        public async Task<ActionResult> Delete(long seasonId, int roundNumber, CancellationToken cancellationToken)
        {
            SeasonModel? season = await _leagueRepository.GetSeasonAsync(seasonId, cancellationToken);
            if (season is null)
            {
                return NotFound();
            }

            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, season, PolicyNames.Manager);
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

            LeagueModel? league = await _leagueRepository.GetLeagueAsync(season.LeagueId, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            SeasonRound? round = await _leagueRepository.GetSeasonRoundAsync(seasonId, roundNumber, cancellationToken);
            if (round is null)
            {
                return NotFound();
            }

            return View(
                new SeasonRoundViewModel
                {
                    Round = round,
                    Season = season,
                    League = league,
                });
        }

        // POST League/Season/{seasonId}/Round/{roundNumber}/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirm(long seasonId, int roundNumber, CancellationToken cancellationToken)
        {
            SeasonModel? season = await _leagueRepository.GetSeasonAsync(seasonId, cancellationToken);
            if (season is null)
            {
                return NotFound();
            }

            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, season, PolicyNames.Manager);
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

            await _leagueRepository.DeleteSeasonRoundAsync(seasonId, roundNumber, cancellationToken);
            return RedirectToAction("Rounds", "Season", new { seasonId });
        }
    }
}

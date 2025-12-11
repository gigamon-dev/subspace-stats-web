using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubspaceStats.Areas.League.Authorization;
using SubspaceStats.Areas.League.Models;
using SubspaceStats.Areas.League.Models.League;
using SubspaceStats.Models;
using SubspaceStats.Services;

namespace SubspaceStats.Areas.League.Controllers
{
    [Area("league")]
    public class LeagueController(
        IAuthorizationService authorizationService, 
        ILeagueRepository leagueRepository,
        IStatsRepository statsRepository) : Controller
    {
        private readonly IAuthorizationService _authorizationService = authorizationService;
        private readonly ILeagueRepository _leagueRepository = leagueRepository;
        private readonly IStatsRepository _statsRepository = statsRepository;

        // GET League/Manage
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            Task<List<LeagueNavItem>> navTask = _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
            Task<List<LeagueModel>> leagueListTask = _leagueRepository.GetLeagueListAsync(cancellationToken);
            Task<OrderedDictionary<long, GameType>> gameTypesTask = _statsRepository.GetGameTypesAsync(cancellationToken);

            await Task.WhenAll(navTask, leagueListTask, gameTypesTask);

            return View(
                new LeagueListViewModel()
                {
                    LeagueSeasonChooser = new LeagueSeasonChooserViewModel(navTask.Result, null, null),
                    Leagues = leagueListTask.Result,
                    GameTypes = gameTypesTask.Result,
                });
        }

        // GET: League/{leagueId}
        public async Task<ActionResult> Details(long? leagueId, CancellationToken cancellationToken)
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

            Task<List<LeagueNavItem>> navTask = _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
            Task<List<SeasonListItem>> seasonsTask = _leagueRepository.GetSeasonsAsync(leagueId.Value, cancellationToken);
            Task<OrderedDictionary<long, GameType>> gameTypesTask = _statsRepository.GetGameTypesAsync(cancellationToken);

            await Task.WhenAll(navTask, seasonsTask, gameTypesTask);

            return View(
                new LeagueDetailsViewModel
                {
                    League = league,
                    LeagueSeasonChooser = new LeagueSeasonChooserViewModel(navTask.Result, league.Id, null),
                    GameTypes = gameTypesTask.Result,
                    Seasons = seasonsTask.Result,
                });
        }

        // GET: League/Create
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult> Create(CancellationToken cancellationToken)
        {
            Task<List<LeagueNavItem>> navTask = _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
            Task<OrderedDictionary<long, GameType>> gameTypesTask = _statsRepository.GetGameTypesAsync(cancellationToken);

            await Task.WhenAll(navTask, gameTypesTask);

            return View(
                new LeagueViewModel()
                {
                    League = new()
                    {
                        Name = "",
                        MinTeamsPerGame = 2,
                        MaxTeamsPerGame = 2,
                        FreqStart = 10,
                        FreqIncrement = 10,
                    },
                    LeagueSeasonChooser = new LeagueSeasonChooserViewModel(navTask.Result, null, null),
                    GameTypes = gameTypesTask.Result,
                });
        }

        // POST: League/Create
        [Authorize(Roles = RoleNames.Administrator)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            [Bind("Name", "GameTypeId", "MinTeamsPerGame", "MaxTeamsPerGame", "FreqStart", "FreqIncrement", Prefix ="League")]LeagueModel league, 
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                Task<List<LeagueNavItem>> navTask = _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
                Task<OrderedDictionary<long, GameType>> gameTypesTask = _statsRepository.GetGameTypesAsync(cancellationToken);

                await Task.WhenAll(navTask, gameTypesTask);

                return View(
                    new LeagueViewModel()
                    {
                        League = league,
                        LeagueSeasonChooser = new LeagueSeasonChooserViewModel(navTask.Result, null, null),
                        GameTypes = gameTypesTask.Result,
                    });
            }

            long leagueId = await _leagueRepository.InsertLeagueAsync(
                league.Name!,
                league.GameTypeId,
                league.MinTeamsPerGame,
                league.MaxTeamsPerGame,
                league.FreqStart,
                league.FreqIncrement,
                cancellationToken);

            return RedirectToAction(nameof(Details), "League", new { leagueId });
        }

        // GET: League/{leagueId}/Edit
        public async Task<ActionResult> Edit(long? leagueId, CancellationToken cancellationToken)
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
            Task<OrderedDictionary<long, GameType>> gameTypesTask = _statsRepository.GetGameTypesAsync(cancellationToken);

            await Task.WhenAll(navTask, gameTypesTask);

            return View(
                new LeagueViewModel
                {
                    League = league,
                    LeagueSeasonChooser = new LeagueSeasonChooserViewModel(navTask.Result, league.Id, null),
                    GameTypes = gameTypesTask.Result,
                });
        }

        // POST: League/{leagueId}/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            long? leagueId,
            [Bind("Id", "Name", "GameTypeId", "MinTeamsPerGame", "MaxTeamsPerGame", "FreqStart", "FreqIncrement", Prefix = "League")] LeagueModel league,
            CancellationToken cancellationToken)
        {
            if (leagueId is null || leagueId != league.Id)
            {
                return NotFound();
            }

            LeagueModel? currentLeague = await _leagueRepository.GetLeagueAsync(leagueId.Value, cancellationToken);
            if (currentLeague is null)
            {
                return NotFound();
            }

            AuthorizationResult result = await _authorizationService.AuthorizeAsync(User, currentLeague, PolicyNames.Manager);
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
                Task<List<LeagueNavItem>> navTask = _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
                Task<OrderedDictionary<long, GameType>> gameTypesTask = _statsRepository.GetGameTypesAsync(cancellationToken);

                await Task.WhenAll(navTask, gameTypesTask);

                return View(
                    new LeagueViewModel
                    {
                        League = league,
                        LeagueSeasonChooser = new LeagueSeasonChooserViewModel(navTask.Result, league.Id, null),
                        GameTypes = gameTypesTask.Result,
                    });
            }

            await _leagueRepository.UpdateLeagueAsync(league, cancellationToken);
            return RedirectToAction(nameof(Details));
        }

        // GET: League/{leagueId}/Delete
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<ActionResult> Delete(long? leagueId, CancellationToken cancellationToken)
        {
            if (leagueId is null)
            {
                return NotFound();
            }

            var league = await _leagueRepository.GetLeagueAsync(leagueId.Value, cancellationToken);
            if (league is null)
            {
                return NotFound();
            }

            Task<List<LeagueNavItem>> navTask = _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken);
            Task<OrderedDictionary<long, GameType>> gameTypesTask = _statsRepository.GetGameTypesAsync(cancellationToken);

            await Task.WhenAll(navTask, gameTypesTask);

            return View(
                new LeagueViewModel
                {
                    League = league,
                    LeagueSeasonChooser = new LeagueSeasonChooserViewModel(navTask.Result, league.Id, null),
                    GameTypes = gameTypesTask.Result,
                });
        }

        // POST: League/{leagueId}/Delete
        [Authorize(Roles = RoleNames.Administrator)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long? leagueId, CancellationToken cancellationToken)
        {
            if (leagueId is null)
            {
                return NotFound();
            }

            await _leagueRepository.DeleteLeagueAsync(leagueId.Value, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
    }
}

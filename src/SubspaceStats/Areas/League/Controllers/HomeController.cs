using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SubspaceStats.Areas.League.Models;
using SubspaceStats.Options;
using SubspaceStats.Services;

namespace SubspaceStats.Areas.League.Controllers
{
    [Area("League")]
    public class HomeController(
        IOptions<LeagueOptions> options, 
        ILeagueRepository leagueRepository) : Controller
    {
        private readonly IOptions<LeagueOptions> _options = options;
        private readonly ILeagueRepository _leagueRepository = leagueRepository;

        // GET League
        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            // TODO: add scheduled games across all configured LeagueIds

            // Show latest standings for the configured LeagueIds
            return View(
                new HomeViewModel
                {
                    Standings = await _leagueRepository.GetLatestSeasonsStandingsAsync(_options.Value.LeagueIds, cancellationToken),
                });
        }

        // GET League/Nav
        public async Task<ActionResult> Nav(long? leagueId, long? seasonId, CancellationToken cancellationToken)
        {
            // For users that have javascript disabled, the LeagueSeasonChooser (partial view cascasding dropdowns) will not work.
            // Instead, they will see the dropdowns as disabled and the GO button will take them here to a page that lists all of the leagues and their seasons in a tree form.
            // The parameters are hints at where to automatically scroll to in the tree.
            return View(
                new NavViewModel
                {
                    LeagueWithSeasons = await _leagueRepository.GetLeaguesWithSeasonsAsync(cancellationToken),
                    LeagueId = leagueId,
                    SeasonId = seasonId,
                });
        }
    }
}

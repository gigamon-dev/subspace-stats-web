using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubspaceStats.Areas.League.Authorization;
using SubspaceStats.Areas.League.Models.Franchise;
using SubspaceStats.Services;

namespace SubspaceStats.Areas.League.Controllers
{
    [Authorize(Roles = RoleNames.Administrator)]
    [Area("League")]
    public class FranchiseController(
        ILeagueRepository leagueRepository) : Controller
    {
        private readonly ILeagueRepository _leagueRepository = leagueRepository;

        // GET League/Franchise
        [AllowAnonymous]
        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            return View(
                new FranchiseListViewModel
                {
                    FranchiseList = await _leagueRepository.GetFranchiseListAsync(cancellationToken)
                });
        }

        // GET League/Franchise/{franchiseId}
        [AllowAnonymous]
        public async Task<ActionResult> Details(long? franchiseId, CancellationToken cancellationToken)
        {
            if (franchiseId is null)
            {
                return NotFound();
            }

            FranchiseModel? franchise = await _leagueRepository.GetFranchiseAsync(franchiseId.Value, cancellationToken);
            if (franchise is null)
            {
                return NotFound();
            }

            return View(
                new FranchiseDetailsViewModel
                {
                    Franchise = franchise,
                    TeamsAndSeasons = await _leagueRepository.GetFranchiseTeamsAsync(franchiseId.Value, cancellationToken),
                });
        }

        // GET League/Franchise/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST League/Franchise/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind("Name")] FranchiseModel franchise, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(franchise);
            }

            long franchiseId = await _leagueRepository.InsertFranchiseAsync(franchise.Name, cancellationToken);
            return RedirectToAction(nameof(Details), new { franchiseId });
        }

        // GET League/Franchise/{franchiseId}/Edit
        public async Task<ActionResult> Edit(long? franchiseId, CancellationToken cancellationToken)
        {
            if (franchiseId is null)
            {
                return NotFound();
            }

            FranchiseModel? franchise = await _leagueRepository.GetFranchiseAsync(franchiseId.Value, cancellationToken);
            if(franchise is null)
            {
                return NotFound();
            }

            return View(franchise);
        }

        // POST League/Franchise/{franchiseId}/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long? franchiseId, [Bind("Id", "Name")] FranchiseModel franchise, CancellationToken cancellationToken)
        {
            if (franchiseId is null || franchiseId != franchise.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(franchise);
            }

            await _leagueRepository.UpdateFranchiseAsync(franchise, cancellationToken);
            return RedirectToAction(nameof(Details), new { id = franchise.Id });
        }

        // GET League/Franchise/{franchiseId}/Delete
        public async Task<ActionResult> Delete(long? franchiseId, CancellationToken cancellationToken)
        {
            if (franchiseId is null)
            {
                return NotFound();
            }

            FranchiseModel? franchise = await _leagueRepository.GetFranchiseAsync(franchiseId.Value, cancellationToken);
            if (franchise is null)
            {
                return NotFound();
            }

            return View(franchise);
        }

        // POST League/Franchise/{franchiseId}/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long? franchiseId, CancellationToken cancellationToken)
        {
            if (franchiseId is null)
            {
                return NotFound();
            }

            await _leagueRepository.DeleteFranchiseAsync(franchiseId.Value, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
    }
}

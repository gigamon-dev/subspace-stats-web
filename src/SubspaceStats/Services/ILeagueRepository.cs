using SubspaceStats.Areas.League.Authorization;
using SubspaceStats.Areas.League.Models;
using SubspaceStats.Areas.League.Models.Franchise;
using SubspaceStats.Areas.League.Models.League;
using SubspaceStats.Areas.League.Models.League.Roles;
using SubspaceStats.Areas.League.Models.Season;
using SubspaceStats.Areas.League.Models.Season.Game;
using SubspaceStats.Areas.League.Models.Season.Player;
using SubspaceStats.Areas.League.Models.Season.Roles;
using SubspaceStats.Areas.League.Models.Season.Round;
using SubspaceStats.Areas.League.Models.Season.Team;
using SubspaceStats.Areas.League.Models.SeasonGame;

namespace SubspaceStats.Services
{
    public interface ILeagueRepository
    {
        Task<List<SeasonStandings>> GetLatestSeasonsStandingsAsync(long[] leagueIds, CancellationToken cancellationToken);
        Task<List<SeasonRoster>> GetSeasonRostersAsync(long seasonId, CancellationToken cancellationToken);
        Task<List<TeamStanding>> GetSeasonStandingsAsync(long seasonId, CancellationToken cancellationToken);
        Task<List<ScheduledGame>> GetScheduledGamesAsync(long seasonId, CancellationToken cancellationToken);
        Task<List<GameRecord>> GetCompletedGamesAsync(long seasonId, CancellationToken cancellationToken);
        Task<List<LeagueNavItem>> GetLeaguesWithSeasonsAsync(CancellationToken cancellationToken);

        #region Franchise

        Task<OrderedDictionary<long, FranchiseModel>> GetFranchisesAsync(CancellationToken cancellationToken);
        Task<List<FranchiseListItem>> GetFranchiseListAsync(CancellationToken cancellationToken);
        Task<List<TeamAndSeason>> GetFranchiseTeamsAsync(long franchiseId, CancellationToken cancellationToken);
        Task<FranchiseModel?> GetFranchiseAsync(long franchiseId, CancellationToken cancellationToken);
        Task<long> InsertFranchiseAsync(string franchiseName, CancellationToken cancellationToken);
        Task UpdateFranchiseAsync(FranchiseModel franchise, CancellationToken cancellationToken);
        Task DeleteFranchiseAsync(long franchiseId, CancellationToken cancellationToken);

        #endregion

        #region League

        Task<List<LeagueModel>> GetLeagueListAsync(CancellationToken cancellationToken);
        Task<LeagueModel?> GetLeagueAsync(long leagueId, CancellationToken cancellationToken);
        Task<long> InsertLeagueAsync(string name, long gameType, short minTeamsPerGame, short maxTeamsPerGame, short freqStart, short freqIncrement, CancellationToken cancellationToken);
        Task UpdateLeagueAsync(LeagueModel league, CancellationToken cancellationToken);
        Task DeleteLeagueAsync(long leagueId, CancellationToken cancellationToken);
        Task<List<Areas.League.Models.League.SeasonListItem>> GetSeasonsAsync(long leagueId, CancellationToken cancellationToken);

        #endregion

        #region Season

        Task<SeasonDetails?> GetSeasonDetailsAsync(long seasonId, CancellationToken cancellationToken);
        Task StartSeasonAsync(long seasonId, DateOnly? startDate, CancellationToken cancellationToken);
        Task EndSeasonAsync(long seasonId, CancellationToken cancellationToken);
        Task UndoEndSeasonAsync(long seasonId, CancellationToken cancellationToken);
        Task<long> CopySeasonAsync(long seasonId, string seasonName, bool includePlayers, bool includeTeams, bool includeGames, bool includeRounds, CancellationToken cancellationToken);
        
        Task<SeasonModel?> GetSeasonAsync(long seasonId, CancellationToken cancellationToken);
        Task<long> InsertSeasonAsync(string seasonName, long leagueId, CancellationToken cancellationToken);
        Task UpdateSeasonAsync(long seasonId, string seasonName, CancellationToken cancellationToken);
        Task DeleteSeasonAsync(long seasonId, CancellationToken cancellationToken);

        #endregion

        #region Season Players

        Task<List<PlayerListItem>> GetSeasonPlayersAsync(long seasonId, CancellationToken cancellationToken);
        Task<SeasonPlayer?> GetSeasonPlayerAsync(long seasonId, string playerName, CancellationToken cancellationToken);
        Task InsertSeasonPlayersAsync(long seasonId, List<string> nameList, long? teamId, CancellationToken cancellationToken);
        Task UpdateSeasonPlayerAsync(long seasonId, SeasonPlayer model, CancellationToken cancellationToken);
        Task DeleteSeasonPlayerAsync(long seasonId, long playerId, CancellationToken cancellationToken);

        // TODO: From list of season players (/league/season/<id>/players), check off the ones to assign, click "Assign Team" button (post),
        // show a page that lists the selection (including if the player already is assigned a team, meaning the team will be switched) and the user selects the team.
        // This should make it easy to assign a single player or multiple players at once.
        // Task AssignToTeamAsync(long teamId, string[] playerNames, CancellationToken cancellationToken)

        #endregion

        #region Season Teams

        Task<OrderedDictionary<long, TeamModel>> GetSeasonTeamsAsync(long seasonId, CancellationToken cancellationToken);
        Task<List<TeamGameRecord>> GetTeamGames(long teamId, CancellationToken cancellationToken);
        Task<List<RosterItem>> GetTeamRoster(long teamId, CancellationToken cancellationToken);
        Task<TeamWithSeasonInfo?> GetTeamsWithSeasonInfoAsync(long teamId, CancellationToken cancellationToken);
        Task<TeamModel?> GetTeamAsync(long teamId, CancellationToken cancellationToken);
        Task<long> InsertTeamAsync(long seasonId, string teamName, string? bannerSmall, string? bannerLarge, long? franchiseId, CancellationToken cancellationToken);
        Task UpdateTeamAsync(long teamId, string teamName, string? bannerSmall, string? bannerLarge, long? franchiseId, CancellationToken cancellationToken);        
        Task DeleteTeamAsync(long teamId, CancellationToken cancellationToken);
        Task RefreshSeasonTeamStatsAsync(long seasonId, CancellationToken cancellationToken);

        #endregion        

        #region Season Games

        Task<List<GameModel>> GetSeasonGamesAsync(long seasonId, CancellationToken cancellationToken);
        Task<GameModel?> GetSeasonGameAsync(long seasonGameId, CancellationToken cancellationToken);
        Task InsertSeasonGamesForRoundWith2TeamsAsync(long seasonId, FullRoundOf mode, CancellationToken cancellationToken);
        Task<long> InsertSeasonGameAsync(long seasonId, GameModel game, CancellationToken cancellationToken);
        Task UpdateSeasonGameAsync(GameModel game, CancellationToken cancellationToken);
        Task DeleteSeasonGame(long seasonGameId, CancellationToken cancellationToken);

        #endregion

        #region Season Rounds

        Task<OrderedDictionary<int, SeasonRound>> GetSeasonRoundsAsync(long seasonId, CancellationToken cancellationToken);
        Task<SeasonRound?> GetSeasonRoundAsync(long seasonId, int roundNumber, CancellationToken cancellationToken);
        Task InsertSeasonRoundAsync(SeasonRound seasonRound, CancellationToken cancellationToken);
        Task UpdateSeasonRoundAsync(SeasonRound seasonRound, CancellationToken cancellationToken);
        Task DeleteSeasonRoundAsync(long seasonId, int roundNumber, CancellationToken cancellationToken);

        #endregion

        #region Authorization

        Task<bool> IsLeagueManager(string userId, long leagueId, CancellationToken cancellationToken);
        Task<bool> IsLeagueOrSeasonManager(string userId, long seasonId, CancellationToken cancellationToken);

        Task<List<LeagueUserRole>> GetLeagueUserRoles(long leagueId, CancellationToken cancellationToken);
        Task InsertLeagueUserRole(long leagueId, string userId, LeagueRole role, CancellationToken cancellationToken);
        Task DeleteLeagueUserRole(long leagueId, string userId, LeagueRole role, CancellationToken cancellationToken);

        Task<List<SeasonUserRole>> GetSeasonUserRoles(long seasonId, CancellationToken cancellationToken);
        Task InsertSeasonUserRole(long seasonId, string userId, SeasonRole role, CancellationToken cancellationToken);
        Task DeleteSeasonUserRole(long seasonId, string userId, SeasonRole role, CancellationToken cancellationToken);

        #endregion
    }
}

using Npgsql;
using NpgsqlTypes;
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
using SubspaceStats.Models;
using System.Data;
using System.Text.Json;

namespace SubspaceStats.Services
{
    public class LeagueRepository : ILeagueRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StatsRepository> _logger;
        private readonly NpgsqlDataSource _dataSource;

        public LeagueRepository(
            IConfiguration configuration,
            ILogger<StatsRepository> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataSource = NpgsqlDataSource.Create(
                _configuration.GetConnectionString("SubspaceStats") ?? throw new Exception("Missing 'SubspaceStats' connection string."));
        }

        public async Task<List<SeasonStandings>> GetLatestSeasonsStandingsAsync(long[] leagueIds, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.get_latest_seasons_with_standings($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(leagueIds);
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new Exception("Expected a row.");

                            if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                                return [];

                            Stream stream = await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false);
                            await using (stream.ConfigureAwait(false))
                            {
                                return await JsonSerializer.DeserializeAsync(stream, SeasonStandingsSourceGenerationContext.Default.ListSeasonStandings, cancellationToken).ConfigureAwait(false) ?? [];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest seasons. (league_ids: {league_ids})", leagueIds);
                throw;
            }
        }

        public async Task<List<SeasonRoster>> GetSeasonRostersAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.get_season_rosters($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new Exception("Expected a row.");

                            if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                                return [];

                            Stream stream = await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false);
                            await using (stream.ConfigureAwait(false))
                            {
                                return await JsonSerializer.DeserializeAsync(stream, SeasonRosterSourceGenerationContext.Default.ListSeasonRoster, cancellationToken).ConfigureAwait(false) ?? [];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rosters for season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<List<TeamStanding>> GetSeasonStandingsAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_standings($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_teamId = reader.GetOrdinal("team_id");
                            int column_teamName = reader.GetOrdinal("team_name");
                            int column_wins = reader.GetOrdinal("wins");
                            int column_losses = reader.GetOrdinal("losses");
                            int column_draws = reader.GetOrdinal("draws");

                            List<TeamStanding> standingList = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                standingList.Add(
                                    new TeamStanding()
                                    {
                                        TeamId = reader.GetInt64(column_teamId),
                                        TeamName = reader.GetString(column_teamName),
                                        Wins = reader.GetInt32(column_wins),
                                        Losses = reader.GetInt32(column_losses),
                                        Draws = reader.GetInt32(column_draws),
                                    });
                            }

                            return standingList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting standings for season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<List<ScheduledGame>> GetScheduledGamesAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_scheduled_games($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_leagueId = reader.GetOrdinal("league_id");
                            int column_leagueName = reader.GetOrdinal("league_name");
                            int column_seasonId = reader.GetOrdinal("season_id");
                            int column_seasonName = reader.GetOrdinal("season_name");
                            int column_seasonGameId = reader.GetOrdinal("season_game_id");
                            int column_roundNumber = reader.GetOrdinal("round_number");
                            int column_roundName = reader.GetOrdinal("round_name");
                            int column_gameTimestamp = reader.GetOrdinal("game_timestamp");
                            int column_teams = reader.GetOrdinal("teams");
                            int column_gameStatusId = reader.GetOrdinal("game_status_id");

                            List<ScheduledGame> gameList = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                gameList.Add(
                                    new ScheduledGame()
                                    {
                                        LeagueId = reader.GetInt64(column_leagueId),
                                        LeagueName = reader.GetString(column_leagueName),
                                        SeasonId = reader.GetInt64(column_seasonId),
                                        SeasonName = reader.GetString(column_seasonName),
                                        SeasonGameId = reader.GetInt64(column_seasonGameId),
                                        RoundNumber = reader.IsDBNull(column_roundNumber) ? null : reader.GetInt32(column_roundNumber),
                                        RoundName = reader.IsDBNull(column_roundName) ? null : reader.GetString(column_roundName),
                                        GameTimestamp = reader.IsDBNull(column_gameTimestamp) ? null : reader.GetDateTime(column_gameTimestamp),
                                        Teams = reader.GetString(column_teams),
                                        Status = (GameStatus)reader.GetInt64(column_gameStatusId),
                                    });
                            }

                            return gameList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scheduled games for season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<List<GameRecord>> GetCompletedGamesAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.get_completed_games($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new Exception("Expected a row.");

                            if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                                return [];

                            Stream stream = await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false);
                            await using (stream.ConfigureAwait(false))
                            {
                                return await JsonSerializer.DeserializeAsync(stream, GameRecordSourceGenerationContext.Default.ListGameRecord, cancellationToken).ConfigureAwait(false) ?? [];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed games. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<List<LeagueNavItem>> GetLeaguesWithSeasonsAsync(CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.get_leagues_with_seasons()", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new Exception("Expected a row.");

                            if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                                return [];

                            Stream stream = await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false);
                            await using (stream.ConfigureAwait(false))
                            {
                                return await JsonSerializer.DeserializeAsync(stream, SeasonNavSourceGenerationContext.Default.ListLeagueNavItem, cancellationToken).ConfigureAwait(false) ?? [];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leagues with seasons.");
                throw;
            }
        }

        public async Task<OrderedDictionary<long, FranchiseModel>> GetFranchisesAsync(CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_franchises();", connection);
                    await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                    await using (command.ConfigureAwait(false))
                    {
                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_franchiseId = reader.GetOrdinal("franchise_id");
                            int column_franchiseName = reader.GetOrdinal("franchise_name");

                            OrderedDictionary<long, FranchiseModel> dictionary = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                var franchise = new FranchiseModel()
                                {
                                    Id = reader.GetInt64(column_franchiseId),
                                    Name = reader.GetString(column_franchiseName),
                                };

                                dictionary.Add(franchise.Id, franchise);
                            }

                            return dictionary;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting franchises.");
                throw;
            }
        }

        public async Task<List<FranchiseListItem>> GetFranchiseListAsync(CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_franchises_with_teams();", connection);
                    await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                    await using (command.ConfigureAwait(false))
                    {
                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_franchiseId = reader.GetOrdinal("franchise_id");
                            int column_franchiseName = reader.GetOrdinal("franchise_name");
                            int column_teams = reader.GetOrdinal("teams");

                            List<FranchiseListItem> itemList = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                itemList.Add(new FranchiseListItem()
                                {
                                    Id = reader.GetInt64(column_franchiseId),
                                    Name = reader.GetString(column_franchiseName),
                                    Teams = reader.IsDBNull(column_teams) ? [] : reader.GetFieldValue<string[]>(column_teams),
                                });
                            }

                            return itemList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting franchises with teams.");
                throw;
            }
        }

        public async Task<FranchiseModel?> GetFranchiseAsync(long franchiseId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_franchise($1);", connection);
                    command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = franchiseId });
                    await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                    await using (command.ConfigureAwait(false))
                    {
                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                return null;

                            return new FranchiseModel
                            {
                                Id = franchiseId,
                                Name = reader.GetString("franchise_name"),
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting franchise. (franchise_id: {franchise_id})", franchiseId);
                throw;
            }
        }

        public async Task<List<TeamAndSeason>> GetFranchiseTeamsAsync(long franchiseId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_franchise_teams($1);", connection);
                    command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = franchiseId });
                    await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                    await using (command.ConfigureAwait(false))
                    {
                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_teamId = reader.GetOrdinal("team_id");
                            int column_teamName = reader.GetOrdinal("team_name");
                            int column_seasonId = reader.GetOrdinal("season_id");
                            int column_seasonName = reader.GetOrdinal("season_name");
                            int column_leagueId = reader.GetOrdinal("league_id");
                            int column_leagueName = reader.GetOrdinal("league_name");

                            List<TeamAndSeason> itemList = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                itemList.Add(new TeamAndSeason()
                                {
                                    TeamId = reader.GetInt64(column_teamId),
                                    TeamName = reader.GetString(column_teamName),
                                    SeasonId = reader.GetInt64(column_seasonId),
                                    SeasonName = reader.GetString(column_seasonName),
                                    LeagueId = reader.GetInt64(column_leagueId),
                                    LeagueName = reader.GetString(column_leagueName),
                                });
                            }

                            return itemList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting franchise teams. (franchise_id: {franchise_id})", franchiseId);
                throw;
            }
        }

        public async Task<long> InsertFranchiseAsync(string franchiseName, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_franchise($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(franchiseName);
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        object? franchiseIdObj = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (franchiseIdObj is null || franchiseIdObj == DBNull.Value)
                            throw new Exception("Expected an value.");

                        return (long)franchiseIdObj;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting franchise. (franchise_name:{franchise_name})", franchiseName);
                throw;
            }
        }

        public async Task UpdateFranchiseAsync(FranchiseModel franchise, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.update_franchise($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = franchise.Id });
                        command.Parameters.AddWithValue(franchise.Name);
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating franchise. (franchise_id: {franchise_id}, franchise_name: {franchise_name})", franchise.Id, franchise.Name);
                throw;
            }
        }

        public async Task DeleteFranchiseAsync(long franchiseId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_franchise($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = franchiseId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting league. (franchise_id: {franchise_id})", franchiseId);
                throw;
            }
        }

        public async Task<List<LeagueModel>> GetLeagueListAsync(CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_leagues();", connection);
                    await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                    await using (command.ConfigureAwait(false))
                    {
                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_leagueId = reader.GetOrdinal("league_id");
                            int column_leagueName = reader.GetOrdinal("league_name");
                            int column_gameTypeId = reader.GetOrdinal("game_type_id");

                            List<LeagueModel> leagueList = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                leagueList.Add(new LeagueModel()
                                {
                                    Id = reader.GetInt64(column_leagueId),
                                    Name = reader.GetString(column_leagueName),
                                    GameTypeId = reader.GetInt64(column_gameTypeId),
                                });
                            }

                            return leagueList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting league list.");
                throw;
            }
        }

        public async Task<LeagueModel?> GetLeagueAsync(long leagueId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_league($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = leagueId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_leagueName = reader.GetOrdinal("league_name");
                            int column_gameTypeId = reader.GetOrdinal("game_type_id");
                            int column_minTeamsPerGame = reader.GetOrdinal("min_teams_per_game");
                            int column_maxTeamsPerGame = reader.GetOrdinal("max_teams_per_game");
                            int column_freqStart = reader.GetOrdinal("freq_start");
                            int column_freqIncrement = reader.GetOrdinal("freq_increment");

                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                return null;

                            return new LeagueModel()
                            {
                                Id = leagueId,
                                Name = reader.GetString(column_leagueName),
                                GameTypeId = reader.GetInt64(column_gameTypeId),
                                MinTeamsPerGame = reader.GetInt16(column_minTeamsPerGame),
                                MaxTeamsPerGame = reader.GetInt16(column_maxTeamsPerGame),
                                FreqStart = reader.GetInt16(column_freqStart),
                                FreqIncrement = reader.GetInt16(column_freqIncrement),
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting league info. (league_id: {league_id})", leagueId);
                throw;
            }
        }

        public async Task<long> InsertLeagueAsync(string name, long gameTypeId, short minTeamsPerGame, short maxTeamsPerGame, short freqStart, short freqIncrement, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_league($1,$2,$3,$4,$5,$6)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(name);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = gameTypeId });
                        command.Parameters.Add(new NpgsqlParameter<short> { TypedValue = minTeamsPerGame });
                        command.Parameters.Add(new NpgsqlParameter<short> { TypedValue = maxTeamsPerGame });
                        command.Parameters.Add(new NpgsqlParameter<short> { TypedValue = freqStart });
                        command.Parameters.Add(new NpgsqlParameter<short> { TypedValue = freqIncrement });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        object? leagueIdObj = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (leagueIdObj is null || leagueIdObj == DBNull.Value)
                            throw new Exception("Expected an value.");

                        return (long)leagueIdObj;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting league. (league_name:{league_name}, game_type_id:{game_type_id})", name, gameTypeId);
                throw;
            }
        }

        public async Task UpdateLeagueAsync(LeagueModel league, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.update_league($1,$2,$3,$4,$5,$6,$7)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = league.Id });
                        command.Parameters.AddWithValue(league.Name);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = league.GameTypeId });
                        command.Parameters.Add(new NpgsqlParameter<short> { TypedValue = league.MinTeamsPerGame });
                        command.Parameters.Add(new NpgsqlParameter<short> { TypedValue = league.MaxTeamsPerGame });
                        command.Parameters.Add(new NpgsqlParameter<short> { TypedValue = league.FreqStart });
                        command.Parameters.Add(new NpgsqlParameter<short> { TypedValue = league.FreqIncrement });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating league. (league_id: {league_id}, league_name: {league_name}, game_type_id:{game_type_id})", league.Id, league.Name, league.GameTypeId);
                throw;
            }
        }

        public async Task DeleteLeagueAsync(long leagueId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_league($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = leagueId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting league. (league_id: {league_id})", leagueId);
                throw;
            }
        }

        public async Task<List<Areas.League.Models.League.SeasonListItem>> GetSeasonsAsync(long leagueId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_seasons($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = leagueId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_seasonId = reader.GetOrdinal("season_id");
                            int column_seasonName = reader.GetOrdinal("season_name");
                            int column_createdTimestamp = reader.GetOrdinal("created_timestamp");

                            List<Areas.League.Models.League.SeasonListItem> seasonList = new(32);
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                seasonList.Add(
                                    new Areas.League.Models.League.SeasonListItem()
                                    {
                                        Id = reader.GetInt64(column_seasonId),
                                        Name = reader.GetString(column_seasonName),
                                        CreatedTimestamp = reader.GetDateTime(column_createdTimestamp),
                                    });
                            }

                            return seasonList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seasons for league. (league_id: {league_id})", leagueId);
                throw;
            }
        }

        public async Task<SeasonDetails?> GetSeasonDetailsAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_details($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_seasonName = reader.GetOrdinal("season_name");
                            int column_leagueId = reader.GetOrdinal("league_id");
                            int column_leagueName = reader.GetOrdinal("league_name");
                            int column_createdTimestamp = reader.GetOrdinal("created_timestamp");
                            int column_startDate = reader.GetOrdinal("start_date");
                            int column_endDate = reader.GetOrdinal("end_date");
                            int column_statPeriodId = reader.GetOrdinal("stat_period_id");
                            int column_statPeriodRange = reader.GetOrdinal("stat_period_range");
                            int column_leagueGameTypeId = reader.GetOrdinal("league_game_type_id");
                            int column_leagueGameTypeName = reader.GetOrdinal("league_game_type_name");
                            int column_leagueGameModeId = reader.GetOrdinal("league_game_mode_id");
                            int column_statsGameTypeId = reader.GetOrdinal("stats_game_type_id");
                            int column_statsGameTypeName = reader.GetOrdinal("stats_game_type_name");
                            int column_statsGameModeId = reader.GetOrdinal("stats_game_mode_id");
                            
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                return null;

                            return new SeasonDetails
                            {
                                LeagueId = reader.GetInt64(column_leagueId),
                                LeagueName = reader.GetString(column_leagueName),
                                SeasonId = seasonId,
                                SeasonName = reader.GetString(column_seasonName),
                                CreatedTimestamp = reader.GetDateTime(column_createdTimestamp),
                                StartDate = reader.IsDBNull(column_startDate) ? null : reader.GetFieldValue<DateOnly>(column_startDate),
                                EndDate = reader.IsDBNull(column_endDate) ? null : reader.GetFieldValue<DateOnly>(column_endDate),
                                StatPeriodId = reader.IsDBNull(column_statPeriodId) ? null : reader.GetInt64(column_statPeriodId),
                                StatPeriodRange = reader.IsDBNull(column_statPeriodRange) ? null : reader.GetFieldValue<NpgsqlRange<DateTime>>(column_statPeriodRange),
                                LeagueGameType = new GameType()
                                {
                                    Id = reader.GetInt64(column_leagueGameTypeId),
                                    Name = reader.GetString(column_leagueGameTypeName),
                                    GameMode = (GameMode)reader.GetInt64(column_leagueGameModeId),
                                },
                                StatsGameType = reader.IsDBNull(column_statsGameTypeId) ? null : new GameType()
                                {
                                    Id = reader.GetInt64(column_statsGameTypeId),
                                    Name = reader.GetString(column_statsGameTypeName),
                                    GameMode = (GameMode)reader.GetInt64(column_statsGameModeId),
                                },
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season details. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task StartSeasonAsync(long seasonId, DateOnly? startDate, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.start_season($1,$2);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });

                        if (startDate is not null)
                            command.Parameters.Add(new NpgsqlParameter<DateOnly> { TypedValue = startDate.Value });
                        else
                            command.Parameters.AddWithValue(DBNull.Value);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task EndSeasonAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.end_season($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task UndoEndSeasonAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.undo_end_season($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error undoing end season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<long> CopySeasonAsync(long seasonId, string seasonName, bool includePlayers, bool includeTeams, bool includeGames, bool includeRounds, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.copy_season($1,$2,$3,$4,$5,$6)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.AddWithValue(seasonName);
                        command.Parameters.Add(new NpgsqlParameter<bool> { TypedValue = includePlayers });
                        command.Parameters.Add(new NpgsqlParameter<bool> { TypedValue = includeTeams });
                        command.Parameters.Add(new NpgsqlParameter<bool> { TypedValue = includeGames });
                        command.Parameters.Add(new NpgsqlParameter<bool> { TypedValue = includeRounds });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new Exception("Expected a result");

                            return reader.GetInt64(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<SeasonModel?> GetSeasonAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_seasonName = reader.GetOrdinal("season_name");
                            int column_leagueId = reader.GetOrdinal("league_id");

                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                return null;

                            return new SeasonModel
                            {
                                SeasonId = seasonId,
                                SeasonName = reader.GetString(column_seasonName),
                                LeagueId = reader.GetInt64(column_leagueId),
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<long> InsertSeasonAsync(string seasonName, long leagueId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_season($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(seasonName);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = leagueId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new Exception("Expected a result");

                            return reader.GetInt64(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting season. (league_id: {league_id})", leagueId);
                throw;
            }
        }

        public async Task UpdateSeasonAsync(long seasonId, string seasonName, CancellationToken  cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.update_season($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.AddWithValue(seasonName);
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task DeleteSeasonAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_season($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting season. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<OrderedDictionary<long, TeamModel>> GetSeasonTeamsAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_teams($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_teamId = reader.GetOrdinal("team_id");
                            int column_teamName = reader.GetOrdinal("team_name");
                            int column_bannerSmall = reader.GetOrdinal("banner_small");
                            int column_bannerLarge = reader.GetOrdinal("banner_large");
                            int column_isEnabled = reader.GetOrdinal("is_enabled");
                            int column_franchiseId = reader.GetOrdinal("franchise_id");

                            OrderedDictionary<long, TeamModel> teams = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                TeamModel team = new()
                                {
                                    TeamId = reader.GetInt64(column_teamId),
                                    TeamName = reader.GetString(column_teamName),
                                    SeasonId = seasonId,
                                    BannerSmall = reader.IsDBNull(column_bannerSmall) ? null : reader.GetString(column_bannerSmall),
                                    BannerLarge = reader.IsDBNull(column_bannerLarge) ? null : reader.GetString(column_bannerLarge),
                                    IsEnabled = reader.GetBoolean(column_isEnabled),
                                    FranchiseId = reader.IsDBNull(column_franchiseId) ? null : reader.GetInt64(column_franchiseId),
                                };

                                teams.Add(team.TeamId, team);
                            }

                            return teams;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season teams. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<List<PlayerListItem>> GetSeasonPlayersAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_players($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_playerId = reader.GetOrdinal("player_id");
                            int column_playerName = reader.GetOrdinal("player_name");
                            int column_signupTimestamp = reader.GetOrdinal("signup_timestamp");
                            int column_teamId = reader.GetOrdinal("team_id");
                            int column_enrollTimestamp = reader.GetOrdinal("enroll_timestamp");
                            int column_isCaptain = reader.GetOrdinal("is_captain");
                            int column_isSuspended = reader.GetOrdinal("is_suspended");

                            List<PlayerListItem> teams = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                teams.Add(new PlayerListItem()
                                {
                                    Id = reader.GetInt64(column_playerId),
                                    Name = reader.GetString(column_playerName),
                                    SignupTimestamp = reader.IsDBNull(column_signupTimestamp) ? null : reader.GetDateTime(column_signupTimestamp),
                                    TeamId = reader.IsDBNull(column_teamId) ? null : reader.GetInt64(column_teamId),
                                    EnrollTimestamp = reader.IsDBNull(column_enrollTimestamp) ? null : reader.GetDateTime(column_enrollTimestamp),
                                    IsCaptain = reader.GetBoolean(column_isCaptain),
                                    IsSuspended = reader.GetBoolean(column_isSuspended),
                                });
                            }

                            return teams;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season players. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<SeasonPlayer?> GetSeasonPlayerAsync(long seasonId, string playerName, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_player($1,$2);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.AddWithValue(playerName);
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_playerId = reader.GetOrdinal("player_id");
                            int column_playerName = reader.GetOrdinal("player_name");
                            int column_teamId = reader.GetOrdinal("team_id");
                            int column_isCaptain = reader.GetOrdinal("is_captain");
                            int column_isSuspended = reader.GetOrdinal("is_suspended");

                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                return null;
                            }

                            return new SeasonPlayer
                            {
                                PlayerId = reader.GetInt64(column_playerId),
                                PlayerName = reader.GetString(column_playerName),
                                TeamId = reader.IsDBNull(column_teamId) ? null : reader.GetInt64(column_teamId),
                                IsCaptain = reader.GetBoolean(column_isCaptain),
                                IsSuspended = reader.GetBoolean(column_isSuspended),
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season player. (season_Id: {season_Id}, player_name: {player_name})", seasonId, playerName);
                throw;
            }
        }

        public async Task InsertSeasonPlayersAsync(long seasonId, List<string> nameList, long? teamId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_season_players($1,$2,$3)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.AddWithValue(nameList);

                        if (teamId is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = teamId.Value });

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting season players. (season_id: {season_id})", seasonId);

                throw;
            }
        }

        public async Task UpdateSeasonPlayerAsync(long seasonId, SeasonPlayer model, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.update_season_player($1,$2,$3,$4,$5)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.AddWithValue(model.PlayerId);

                        if (model.TeamId is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = model.TeamId.Value });

                        command.Parameters.Add(new NpgsqlParameter<bool> { TypedValue = model.IsCaptain });
                        command.Parameters.Add(new NpgsqlParameter<bool> { TypedValue = model.IsSuspended });

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating season player. (season_id: {season_id}, player_id: {player_id})", seasonId, model.PlayerId);

                throw;
            }
        }

        public async Task DeleteSeasonPlayerAsync(long seasonId, long playerId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_season_player($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.AddWithValue(playerId);
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting season player. (season_id: {season_id}, player_id: {player_id})", seasonId, playerId);

                throw;
            }
        }

        public async Task<List<TeamGameRecord>> GetTeamGames(long teamId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_team_games($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = teamId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_seasonGameId = reader.GetOrdinal("season_game_id");
                            int column_roundNumber = reader.GetOrdinal("round_number");
                            int column_roundName = reader.GetOrdinal("round_name");
                            int column_gameTimestamp = reader.GetOrdinal("game_timestamp");
                            int column_gameId = reader.GetOrdinal("game_id");
                            int column_teams = reader.GetOrdinal("teams");
                            int column_winLoseDraw = reader.GetOrdinal("win_lose_draw");
                            int column_scores = reader.GetOrdinal("scores");

                            List<TeamGameRecord> records = new(32);
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                long seasonGameId = reader.GetInt64(column_seasonGameId);
                                int? roundNumber = reader.IsDBNull(column_roundNumber) ? null : reader.GetInt32(column_roundNumber);
                                string? roundName = reader.IsDBNull(column_roundName) ? null : reader.GetString(column_roundName);
                                DateTime? gameTimestamp = reader.IsDBNull(column_gameTimestamp) ? null : reader.GetDateTime(column_gameTimestamp);
                                long? gameId = reader.IsDBNull(column_gameId) ? null : reader.GetInt64(column_gameId);
                                string teams = reader.GetString(column_teams);
                                char? winLoseDraw = reader.IsDBNull(column_winLoseDraw) ? null : reader.GetChar(column_winLoseDraw);
                                GameResult? result = winLoseDraw is null
                                    ? null
                                    : winLoseDraw.Value switch
                                    {
                                        'W' => GameResult.Win,
                                        'L' => GameResult.Loss,
                                        'D' => GameResult.Draw,
                                        _ => null
                                    };

                                string? scores = reader.IsDBNull(column_scores) ? null : reader.GetString(column_scores);

                                records.Add(
                                    new TeamGameRecord()
                                    {
                                        SeasonGameId = seasonGameId,
                                        RoundNumber = roundNumber,
                                        RoundName = roundName,
                                        GameTimestamp = gameTimestamp,
                                        GameId = gameId,
                                        Teams = teams,
                                        Result = result,
                                        Scores = scores,
                                    });
                            }

                            return records;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting games for team. (team_id: {team_id})", teamId);
                throw;
            }
        }

        public async Task<List<RosterItem>> GetTeamRoster(long teamId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_team_roster($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = teamId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_playerId = reader.GetOrdinal("player_id");
                            int column_playerName = reader.GetOrdinal("player_name");
                            int column_isCaptain = reader.GetOrdinal("is_captain");
                            int column_isSuspended = reader.GetOrdinal("is_suspended");
                            int column_enrollTimestamp = reader.GetOrdinal("enroll_timestamp");

                            List<RosterItem> roster = new(32);
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                roster.Add(
                                    new RosterItem()
                                    {
                                        PlayerId = reader.GetInt64(column_playerId),
                                        PlayerName = reader.GetString(column_playerName),
                                        IsCaptain = reader.GetBoolean(column_isCaptain),
                                        IsSuspended = reader.GetBoolean(column_isSuspended),
                                        EnrollTimestamp = reader.IsDBNull(column_enrollTimestamp) ? null : reader.GetDateTime(column_enrollTimestamp),
                                    });
                            }

                            return roster;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roster for team. (team_id: {team_id})", teamId);
                throw;
            }
        }

        public async Task<TeamWithSeasonInfo?> GetTeamsWithSeasonInfoAsync(long teamId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_team_with_season_info($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = teamId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_teamName = reader.GetOrdinal("team_name");
                            int column_bannerSmall = reader.GetOrdinal("banner_small");
                            int column_bannerLarge = reader.GetOrdinal("banner_large");
                            int column_isEnabled = reader.GetOrdinal("is_enabled");
                            int column_franchiseId = reader.GetOrdinal("franchise_id");
                            int column_franchiseName = reader.GetOrdinal("franchise_name");
                            int column_leagueId = reader.GetOrdinal("league_id");
                            int column_leagueName = reader.GetOrdinal("league_name");
                            int column_seasonId = reader.GetOrdinal("season_id");
                            int column_seasonName = reader.GetOrdinal("season_name");
                            int column_wins = reader.GetOrdinal("wins");
                            int column_losses = reader.GetOrdinal("losses");
                            int column_draws = reader.GetOrdinal("draws");

                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                return null;

                            return new TeamWithSeasonInfo
                            {
                                TeamId = teamId,
                                TeamName = reader.GetString("team_name"),
                                BannerSmall = reader.IsDBNull(column_bannerSmall) ? null : reader.GetString(column_bannerSmall),
                                BannerLarge = reader.IsDBNull(column_bannerLarge) ? null : reader.GetString(column_bannerLarge),
                                IsEnabled = reader.GetBoolean(column_isEnabled),
                                FranchiseId = reader.IsDBNull(column_franchiseId) ? null : reader.GetInt64(column_franchiseId),
                                FranchiseName = reader.IsDBNull(column_franchiseName) ? null : reader.GetString(column_franchiseName),
                                LeagueId = reader.GetInt64(column_leagueId),
                                LeagueName = reader.GetString(column_leagueName),
                                SeasonId = reader.GetInt64(column_seasonId),
                                SeasonName = reader.GetString(column_seasonName),
                                Wins = reader.IsDBNull(column_wins) ? null : reader.GetInt32(column_wins),
                                Losses = reader.IsDBNull(column_losses) ? null : reader.GetInt32(column_losses),
                                Draws = reader.IsDBNull(column_draws) ? null : reader.GetInt32(column_draws),
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team with season info. (team_id: {team_id})", teamId);
                throw;
            }
        }

        public async Task<TeamModel?> GetTeamAsync(long teamId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_team($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = teamId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_teamName = reader.GetOrdinal("team_name");
                            int column_seasonId = reader.GetOrdinal("season_id");
                            int column_bannerSmall = reader.GetOrdinal("banner_small");
                            int column_bannerLarge = reader.GetOrdinal("banner_large");
                            int column_isEnabled = reader.GetOrdinal("is_enabled");
                            int column_franchiseId = reader.GetOrdinal("franchise_id");

                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                return null;

                            return new TeamModel
                            {
                                TeamId = teamId,
                                TeamName = reader.GetString(column_teamName),
                                SeasonId =reader.GetInt64(column_seasonId),
                                BannerSmall = reader.IsDBNull(column_bannerSmall) ? null : reader.GetString(column_bannerSmall),
                                BannerLarge = reader.IsDBNull(column_bannerLarge) ? null : reader.GetString(column_bannerLarge),
                                IsEnabled = reader.GetBoolean(column_isEnabled),
                                FranchiseId = reader.IsDBNull(column_franchiseId) ? null : reader.GetInt64(column_franchiseId),
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team. (team_id: {team_id})", teamId);
                throw;
            }
        }

        public async Task<long> InsertTeamAsync(long seasonId, string teamName, string? bannerSmall, string? bannerLarge, long? franchiseId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_team($1,$2,$3,$4,$5)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(teamName);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });

                        if (bannerSmall is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.AddWithValue(bannerSmall);

                        if (bannerLarge is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.AddWithValue(bannerLarge);

                        if (franchiseId is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = franchiseId.Value });

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        object? teamIdObj = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (teamIdObj is null || teamIdObj == DBNull.Value)
                            throw new Exception("Expected an value.");

                        return (long)teamIdObj;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting team. (season_id: {season_id}, team_name: {team_name})", seasonId, teamName);

                throw;
            }
        }

        public async Task UpdateTeamAsync(long teamId, string teamName, string? bannerSmall, string? bannerLarge, long? franchiseId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.update_team($1,$2,$3,$4,$5)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = teamId });
                        command.Parameters.AddWithValue(teamName);

                        if (bannerSmall is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.AddWithValue(bannerSmall);

                        if (bannerLarge is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.AddWithValue(bannerLarge);

                        if (franchiseId is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = franchiseId.Value });

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team. (team_id: {team_id})", teamId);

                throw;
            }
        }

        public async Task DeleteTeamAsync(long teamId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_team($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = teamId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team. (team_id: {team_id})", teamId);

                throw;
            }
        }

        public async Task RefreshSeasonTeamStatsAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.refresh_season_team_stats($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing season team stats. (season_id: {season_id})", seasonId);

                throw;
            }
        }

        public async Task<List<GameModel>> GetSeasonGamesAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_games($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                return [];

                            if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                                return [];

                            Stream stream = await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false);
                            await using (stream.ConfigureAwait(false))
                            {
                                return await JsonSerializer.DeserializeAsync(stream, GameModelSourceGenerationContext.Default.ListGameModel, cancellationToken).ConfigureAwait(false) ?? [];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season games. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<GameModel?> GetSeasonGameAsync(long seasonGameId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_game($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonGameId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                return null;

                            if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                                return null;

                            Stream stream = await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false);
                            await using (stream.ConfigureAwait(false))
                            {
                                return await JsonSerializer.DeserializeAsync(stream, GameModelSourceGenerationContext.Default.GameModel, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season game. (season_game_id: {season_game_id})", seasonGameId);
                throw;
            }
        }

        public async Task InsertSeasonGamesForRoundWith2TeamsAsync(long seasonId, FullRoundOf mode, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_season_games_for_round_with_2_teams($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });    
                        command.Parameters.Add(new NpgsqlParameter<bool> { TypedValue = mode == FullRoundOf.Permutations });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting season games for round with 2 teams. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<long> InsertSeasonGameAsync(long seasonId, GameModel game, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_season_game($1,$2,$3,$4,$5)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = game.RoundNumber });

                        if (game.GameTimestamp is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.Add(new NpgsqlParameter<DateTime> { TypedValue = game.GameTimestamp.Value });

                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = (long)game.Status });

                        string teamsJson = JsonSerializer.Serialize(game.Teams, GameModelSourceGenerationContext.Default.ListGameTeamModel);
                        command.Parameters.AddWithValue(NpgsqlDbType.Jsonb, teamsJson);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        object? seasonGameIdObj = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (seasonGameIdObj is null || seasonGameIdObj == DBNull.Value)
                            throw new Exception("Expected an value.");

                        return (long)seasonGameIdObj;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting season game. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task UpdateSeasonGameAsync(GameModel game, CancellationToken cancellationToken)
        {
            if (game.SeasonGameId is null)
                throw new ArgumentException(paramName: nameof(game), message: "SeasonGameId was null.");

            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.update_season_game($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        string gameJson = JsonSerializer.Serialize(game, GameModelSourceGenerationContext.Default.GameModel);
                        command.Parameters.AddWithValue(NpgsqlDbType.Jsonb, gameJson);
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating season game. (season_game_id: {season_game_id})", game.SeasonGameId);
                throw;
            }
        }

        public async Task DeleteSeasonGame(long seasonGameId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_season_game($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonGameId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting season game. (season_game_id: {season_game_id})", seasonGameId);
                throw;
            }
        }

        public async Task<OrderedDictionary<int, SeasonRound>> GetSeasonRoundsAsync(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_rounds($1);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_roundNumber = reader.GetOrdinal("round_number");
                            int column_roundName = reader.GetOrdinal("round_name");
                            int column_roundDescription = reader.GetOrdinal("round_description");

                            OrderedDictionary<int, SeasonRound> dictionary = [];
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                SeasonRound round = new()
                                {
                                    SeasonId = seasonId,
                                    RoundNumber = reader.GetInt32(column_roundNumber),
                                    RoundName = reader.GetString(column_roundName),
                                    RoundDescription = reader.IsDBNull(column_roundDescription) ? null : reader.GetString(column_roundDescription),
                                };

                                dictionary.Add(round.RoundNumber, round);
                            }

                            return dictionary;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season rounds. (season_id: {season_id})", seasonId);
                throw;
            }
        }

        public async Task<SeasonRound?> GetSeasonRoundAsync(long seasonId, int roundNumber, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_round($1,$2);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = roundNumber });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_roundName = reader.GetOrdinal("round_name");
                            int column_roundDescription = reader.GetOrdinal("round_description");

                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                return null;
                            }

                            return new SeasonRound
                            {
                                SeasonId = seasonId,
                                RoundNumber = roundNumber,
                                RoundName = reader.GetString(column_roundName),
                                RoundDescription = reader.IsDBNull(column_roundDescription) ? null : reader.GetString(column_roundDescription),
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season round. (season_id: {season_id}, round_number: {round_number})", seasonId, roundNumber);
                throw;
            }
        }

        public async Task InsertSeasonRoundAsync(SeasonRound seasonRound, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_season_round($1,$2,$3,$4)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonRound.SeasonId });
                        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = seasonRound.RoundNumber });
                        command.Parameters.AddWithValue(seasonRound.RoundName);

                        if (string.IsNullOrWhiteSpace(seasonRound.RoundDescription))
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.AddWithValue(seasonRound.RoundDescription);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inserting season round. (season_id: {season_id}, round_number: {round_number}, round_name: {round_name})",
                    seasonRound.SeasonId,
                    seasonRound.RoundNumber,
                    seasonRound.RoundName);

                throw;
            }
        }

        public async Task UpdateSeasonRoundAsync(SeasonRound seasonRound, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.update_season_round($1,$2,$3,$4)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonRound.SeasonId });
                        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = seasonRound.RoundNumber });
                        command.Parameters.AddWithValue(seasonRound.RoundName);

                        if (string.IsNullOrWhiteSpace(seasonRound.RoundDescription))
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.AddWithValue(seasonRound.RoundDescription);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating season round. (season_id: {season_id}, round_number: {round_number}, round_name: {round_name})",
                    seasonRound.SeasonId,
                    seasonRound.RoundNumber,
                    seasonRound.RoundName);

                throw;
            }
        }

        public async Task DeleteSeasonRoundAsync(long seasonId, int roundNumber, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_season_round($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = roundNumber });
                        
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting season round. (season_id: {season_id}, round_number: {round_number})",
                    seasonId,
                    roundNumber);

                throw;
            }
        }

        public async Task<bool> IsLeagueManager(string userId, long leagueId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.is_league_manager($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(userId);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = leagueId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        object? resultObj = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (resultObj is null || resultObj == DBNull.Value)
                            throw new Exception("Expected an value.");

                        return (bool)resultObj;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting whether a user is a manager of a league. (user_id:{user_id}, league_id:{league_id})", userId, leagueId);
                throw;
            }
        }
        
        public async Task<bool> IsLeagueOrSeasonManager(string userId, long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.is_league_or_season_manager($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(userId);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        object? resultObj = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (resultObj is null || resultObj == DBNull.Value)
                            throw new Exception("Expected an value.");

                        return (bool)resultObj;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting whether a user is a manager of a league or season. (user_id:{user_id}, season_id:{season_id})", userId, seasonId);
                throw;
            }
        }

        public async Task<List<LeagueUserRole>> GetLeagueUserRoles(long leagueId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_league_user_roles($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = leagueId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_userId = reader.GetOrdinal("user_id");
                            int column_leagueRoleId = reader.GetOrdinal("league_role_id");

                            List<LeagueUserRole> list = [];
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                list.Add(
                                    new LeagueUserRole(
                                        reader.GetString(column_userId),
                                        (LeagueRole)reader.GetInt64(column_leagueRoleId)));
                            }
                            return list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting league user roles. (league_id: {league_id})", leagueId);
                throw;
            }
        }

        public async Task InsertLeagueUserRole(long leagueId, string userId, LeagueRole role, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_league_user_role($1,$2,$3)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = leagueId });
                        command.Parameters.AddWithValue(userId);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = (long)role });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting league user role. (league_id: {league_id}, user_id:{user_id}, league_role_id:{league_role_id})", leagueId, userId, (long)role);
                throw;
            }
        }

        public async Task DeleteLeagueUserRole(long leagueId, string userId, LeagueRole role, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_league_user_role($1,$2,$3)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = leagueId });
                        command.Parameters.AddWithValue(userId);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = (long)role });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting league user role. (league_id: {league_id}, user_id:{user_id}, league_role_id:{league_role_id})", leagueId, userId, (long)role);
                throw;
            }
        }

        public async Task<List<SeasonUserRole>> GetSeasonUserRoles(long seasonId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from league.get_season_user_roles($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_userId = reader.GetOrdinal("user_id");
                            int column_seasonRoleId = reader.GetOrdinal("season_role_id");

                            List<SeasonUserRole> list = [];
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                list.Add(
                                    new SeasonUserRole(
                                        reader.GetString(column_userId),
                                        (SeasonRole)reader.GetInt64(column_seasonRoleId)));
                            }
                            return list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season user roles. (league_id: {league_id})", seasonId);
                throw;
            }
        }

        public async Task InsertSeasonUserRole(long seasonId, string userId, SeasonRole role, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.insert_season_user_role($1,$2,$3)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.AddWithValue(userId);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = (long)role });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting season user role. (season_id: {season_id}, user_id:{user_id}, league_role_id:{league_role_id})", seasonId, userId, (long)role);
                throw;
            }
        }

        public async Task DeleteSeasonUserRole(long seasonId, string userId, SeasonRole role, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select league.delete_season_user_role($1,$2,$3)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = seasonId });
                        command.Parameters.AddWithValue(userId);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = (long)role });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting season user role. (season_id: {season_id}, user_id:{user_id}, league_role_id:{league_role_id})", seasonId, userId, (long)role);
                throw;
            }
        }
    }
}

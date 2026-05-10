using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;
using NpgsqlTypes;
using SubspaceStats.Models;
using SubspaceStats.Models.GameDetails;
using SubspaceStats.Models.GameDetails.TeamVersus;
using SubspaceStats.Models.Leaderboard;
using SubspaceStats.Models.Player;
using System.Data;
using System.Text.Json;

namespace SubspaceStats.Services
{
    public class StatsRepository : IStatsRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StatsRepository> _logger;
        private readonly HybridCache _cache;
        private readonly NpgsqlDataSource _dataSource;

        public StatsRepository(
            IConfiguration configuration,
            ILogger<StatsRepository> logger,
            HybridCache cache)
        {
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache;
            _dataSource = NpgsqlDataSource.Create(
                _configuration.GetConnectionString("SubspaceStats") ?? throw new Exception("Missing 'SubspaceStats' connection string."));
        }

        public async Task<OrderedDictionary<long, GameType>> GetGameTypesAsync(CancellationToken cancellationToken)
        {
            return await _cache.GetOrCreateAsync(
                CacheKeys.GameTypes,
                async cancel =>
                {
                    return await GetAndCacheGameTypesAsync(cancellationToken).ConfigureAwait(false);
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<GameType?> GetGameTypeAsync(long gameTypeId, CancellationToken cancellationToken)
        {
            (await GetGameTypesAsync(cancellationToken).ConfigureAwait(false)).TryGetValue(gameTypeId, out GameType? gameType);
            return gameType;
        }

        private async Task<OrderedDictionary<long, GameType>> GetAndCacheGameTypesAsync(CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_game_types()", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_gameTypeId = reader.GetOrdinal("game_type_id");
                            int column_gameTypeName = reader.GetOrdinal("game_type_name");
                            int column_gameModeId = reader.GetOrdinal("game_mode_id");

                            OrderedDictionary<long, GameType> dictionary = new(32);

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                long gameTypeId = reader.GetInt64(column_gameTypeId);
                                string gameTypeName = reader.GetString(column_gameTypeName);
                                long gameModeId = reader.GetInt64(column_gameModeId);

                                dictionary[gameTypeId] = new GameType
                                {
                                    Id = gameTypeId,
                                    Name = gameTypeName,
                                    GameMode = (GameMode)gameModeId,
                                };
                            }

                            return dictionary;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting game types.");
                throw;
            }
        }

        public async Task<long> InsertGameTypeAsync(string name, GameMode mode, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select ss.insert_game_type($1,$2)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(name);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = (long)mode });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        object? gameTypeIdObj = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        if (gameTypeIdObj is null || gameTypeIdObj == DBNull.Value)
                        {
                            throw new Exception("Expected a result.");
                        }

                        return (long)gameTypeIdObj;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting game type. (game_type_name: {game_type_name})", name);
                throw;
            }
        }

        public async Task UpdateGameTypeAsync(long gameTypeId, string name, GameMode mode, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select ss.update_game_type($1,$2,$3)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = gameTypeId });
                        command.Parameters.AddWithValue(name);
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = (long)mode });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game type. (game_type_id: {game_type_id}, game_type_name:{game_type_name})", gameTypeId, name);
                throw;
            }
        }

        public async Task DeleteGameTypeAsync(long gameTypeId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select ss.delete_game_type($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = gameTypeId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting game type. (game_type_id: {game_type_id})", gameTypeId);
                throw;
            }
        }

        public async Task<List<StatPeriod>> GetStatPeriods(long gameTypeId, StatPeriodType? statPeriodType, int? limit, int offset, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_stat_periods($1,$2,$3,$4);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, (long)gameTypeId);

                        if (statPeriodType is null)
                            command.Parameters.AddWithValue(DBNull.Value);
                        else
                            command.Parameters.AddWithValue(NpgsqlDbType.Bigint, (long)statPeriodType);

                        if (limit is not null)
                            command.Parameters.Add(new NpgsqlParameter<int> { Value = limit.Value });
                        else
                            command.Parameters.AddWithValue(DBNull.Value); // no limit (treated as LIMIT ALL)

                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, offset);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_statPeriodId = reader.GetOrdinal("stat_period_id");
                            int column_periodRange = reader.GetOrdinal("period_range");
                            int column_statPeriodTypeId = reader.GetOrdinal("stat_period_type_id");
                            int column_periodExtraName = reader.GetOrdinal("period_extra_name");

                            List<StatPeriod> periodList = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                periodList.Add(new StatPeriod()
                                {
                                    StatPeriodId = reader.GetInt64(column_statPeriodId),
                                    GameTypeId = gameTypeId,
                                    StatPeriodType = (StatPeriodType)reader.GetInt64(column_statPeriodTypeId),
                                    PeriodRange = await reader.GetFieldValueAsync<NpgsqlRange<DateTime>>(column_periodRange, cancellationToken).ConfigureAwait(false),
                                    ExtraName = reader.IsDBNull(column_periodExtraName) ? null : reader.GetString(column_periodExtraName),
                                });
                            }

                            return periodList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stat periods (gameType:{gameType}, statPeriodType:{statPeriodType}).", gameTypeId, statPeriodType);
                throw;
            }
        }

        public async Task<StatPeriod?> GetForeverStatPeriod(long gameTypeId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_stat_periods($1,$2,$3,$4);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, (long)gameTypeId);
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, (long)StatPeriodType.Forever);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, 1);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, 0);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_statPeriodId = reader.GetOrdinal("stat_period_id");
                            int column_periodRange = reader.GetOrdinal("period_range");

                            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                return new StatPeriod()
                                {
                                    StatPeriodId = reader.GetInt64(column_statPeriodId),
                                    GameTypeId = gameTypeId,
                                    StatPeriodType = StatPeriodType.Forever,
                                    PeriodRange = await reader.GetFieldValueAsync<NpgsqlRange<DateTime>>(column_periodRange, cancellationToken).ConfigureAwait(false),
                                };
                            }

                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting forever stat period (gameType:{gameType}).", gameTypeId);
                throw;
            }
        }

        public async Task<List<TeamVersusLeaderboardStats>> GetTeamVersusLeaderboardAsync(long statPeriodId, int limit, int offset, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_team_versus_leaderboard($1,$2,$3);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, statPeriodId);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, limit);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, offset);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_ratingRank = reader.GetOrdinal("rating_rank");
                            int column_playerName = reader.GetOrdinal("player_name");
                            int column_squadName = reader.GetOrdinal("squad_name");
                            int column_rating = reader.GetOrdinal("rating");
                            int column_ratingAvg = reader.GetOrdinal("rating_avg");
                            int column_gamesPlayed = reader.GetOrdinal("games_played");
                            int column_playDuration = reader.GetOrdinal("play_duration");
                            int column_wins = reader.GetOrdinal("wins");
                            int column_losses = reader.GetOrdinal("losses");
                            int column_kills = reader.GetOrdinal("kills");
                            int column_deaths = reader.GetOrdinal("deaths");
                            int column_damageDealt = reader.GetOrdinal("damage_dealt");
                            int column_damageTaken = reader.GetOrdinal("damage_taken");
                            int column_killDamage = reader.GetOrdinal("kill_damage");
                            int column_forcedReps = reader.GetOrdinal("forced_reps");
                            int column_forcedRepDamage = reader.GetOrdinal("forced_rep_damage");
                            int column_assists = reader.GetOrdinal("assists");
                            int column_wastedEnergy = reader.GetOrdinal("wasted_energy");
                            int column_firstOut = reader.GetOrdinal("first_out");

                            List<TeamVersusLeaderboardStats> statsList = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                statsList.Add(new TeamVersusLeaderboardStats()
                                {
                                    RatingRank = reader.GetInt64(column_ratingRank),
                                    PlayerName = reader.GetString(column_playerName),
                                    SquadName = reader.IsDBNull(column_squadName) ? null : reader.GetString(column_squadName),
                                    Rating = reader.GetInt32(column_rating),
                                    RatingAverage = reader.GetFloat(column_ratingAvg),
                                    GamesPlayed = reader.GetInt64(column_gamesPlayed),
                                    PlayDuration = reader.GetTimeSpan(column_playDuration),
                                    Wins = reader.GetInt64(column_wins),
                                    Losses = reader.GetInt64(column_losses),
                                    Kills = reader.GetInt64(column_kills),
                                    Deaths = reader.GetInt64(column_deaths),
                                    DamageDealt = reader.GetInt64(column_damageDealt),
                                    DamageTaken = reader.GetInt64(column_damageTaken),
                                    KillDamage = reader.GetInt64(column_killDamage),
                                    ForcedReps = reader.GetInt64(column_forcedReps),
                                    ForcedRepDamage = reader.GetInt64(column_forcedRepDamage),
                                    Assists = reader.GetInt64(column_assists),
                                    WastedEnergy = reader.GetInt64(column_wastedEnergy),
                                    FirstOut = reader.GetInt64(column_firstOut),
                                });
                            }

                            return statsList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team versus leaderboard (statPeriodId:{statPeriodId}, limit:{limit}, offset:{offset}).", statPeriodId, limit, offset);
                throw;
            }
        }

        public async Task<Game?> GetGameAsync(long gameId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select ss.get_game($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, gameId);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new Exception("Expected a row.");

                            if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                                return null;

                            Stream stream = await reader.GetStreamAsync(0, cancellationToken).ConfigureAwait(false);
                            await using (stream.ConfigureAwait(false))
                            {
                                return await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.Game, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting game details (gameId:{gameId}).", gameId);
                throw;
            }
        }

        public async Task<(PlayerInfo, List<StatPeriod>)?> GetPlayerInfoAndStatPeriods(string playerName, TimeSpan periodCutoff, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlBatch batch = new(connection);
                    await using (batch.ConfigureAwait(false))
                    {
                        batch.BatchCommands.Add(
                            new NpgsqlBatchCommand("select * from ss.get_player_info($1);")
                            {
                                Parameters =
                                {
                                    new NpgsqlParameter() { Value = playerName }
                                }
                            });

                        batch.BatchCommands.Add(
                            new NpgsqlBatchCommand("select * from ss.get_player_stat_periods($1, $2);")
                            {
                                Parameters =
                                {
                                    new NpgsqlParameter() { Value = playerName },
                                    new NpgsqlParameter<TimeSpan> { TypedValue = periodCutoff },
                                }
                            });

                        await batch.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_squadName = reader.GetOrdinal("squad_name");
                            int column_xRes = reader.GetOrdinal("x_res");
                            int column_yRes = reader.GetOrdinal("y_res");

                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                // Player not found.
                                return null;
                            }

                            PlayerInfo playerInfo = new()
                            {
                                SquadName = reader.IsDBNull(column_squadName) ? null : reader.GetString(column_squadName),
                                XRes = reader.IsDBNull(column_xRes) ? null : reader.GetInt16(column_xRes),
                                YRes = reader.IsDBNull(column_yRes) ? null : reader.GetInt16(column_yRes),
                            };

                            if (!await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
                            {
                                throw new Exception("Missing get_player_game_types result.");
                            }

                            int column_statPeriodId = reader.GetOrdinal("stat_period_id");
                            int column_gameTypeId = reader.GetOrdinal("game_type_id");
                            int column_statPeriodTypeId = reader.GetOrdinal("stat_period_type_id");
                            int column_periodRange = reader.GetOrdinal("period_range");
                            int column_periodExtraName = reader.GetOrdinal("period_extra_name");

                            List<StatPeriod> periodList = [];
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                periodList.Add(
                                    new StatPeriod()
                                    {
                                        StatPeriodId = reader.GetInt64(column_statPeriodId),
                                        GameTypeId = reader.GetInt64(column_gameTypeId),
                                        StatPeriodType = (StatPeriodType)reader.GetInt64(column_statPeriodTypeId),
                                        PeriodRange = reader.GetFieldValue<NpgsqlRange<DateTime>>(column_periodRange),
                                        ExtraName = reader.IsDBNull(column_periodExtraName) ? null : reader.GetString(column_periodExtraName),
                                    });
                            }

                            return (playerInfo, periodList);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting player info and stat periods (playerName:{playerName}).", playerName);
                throw;
            }
        }

        public async Task<List<ParticipationRecord>> GetPlayerParticipationOverview(string playerName, TimeSpan periodCutoff, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_player_participation_overview($1,$2);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Varchar, playerName);
                        command.Parameters.AddWithValue(NpgsqlDbType.Interval, periodCutoff);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_statPeriodId = reader.GetOrdinal("stat_period_id");
                            int column_gameTypeId = reader.GetOrdinal("game_type_id");
                            int column_statPeriodTypeId = reader.GetOrdinal("stat_period_type_id");
                            int column_periodRange = reader.GetOrdinal("period_range");
                            int column_periodExtraName = reader.GetOrdinal("period_extra_name");
                            int column_rating = reader.GetOrdinal("rating");
                            // TODO: int column_detailsJson = reader.GetOrdinal("details_json");

                            List<ParticipationRecord> participationRecordList = [];
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                participationRecordList.Add(
                                    new ParticipationRecord()
                                    {
                                        LastStatPeriod = new StatPeriod()
                                        {
                                            StatPeriodId = reader.GetInt64(column_statPeriodId),
                                            GameTypeId = reader.GetInt64(column_gameTypeId),
                                            StatPeriodType = (StatPeriodType)reader.GetInt64(column_statPeriodTypeId),
                                            PeriodRange = reader.GetFieldValue<NpgsqlRange<DateTime>>(column_periodRange),
                                            ExtraName = reader.IsDBNull(column_periodExtraName) ? null : reader.GetString(column_periodExtraName),
                                        },
                                        Rating = reader.IsDBNull(column_rating) ? null : reader.GetInt32(column_rating),
                                        // TODO: json details
                                    });
                            }
                            return participationRecordList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting player participation overview (playerName:{playerName}, periodCutoff:{periodCutoff}).", playerName, periodCutoff);
                throw;
            }
        }

        public async Task<(StatPeriod, List<TopRatingRecord>)?> GetTopPlayersByRating(long gameTypeId, StatPeriodType statPeriodType, int top, CancellationToken cancellationToken)
        {
            List<StatPeriod> statPeriodList = await GetStatPeriods(gameTypeId, statPeriodType, 1, 0, cancellationToken).ConfigureAwait(false);
            if (statPeriodList.Count == 0)
                return null;

            return (statPeriodList[0], await GetTopPlayersByRating(statPeriodList[0].StatPeriodId, top, cancellationToken).ConfigureAwait(false));
        }

        public async Task<List<TopRatingRecord>> GetTopPlayersByRating(long statPeriodId, int top, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_top_players_by_rating($1,$2);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, statPeriodId);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, top);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_topRank = reader.GetOrdinal("top_rank");
                            int column_playerName = reader.GetOrdinal("player_name");
                            int column_rating = reader.GetOrdinal("rating");

                            List<TopRatingRecord> topList = new(top);

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                topList.Add(
                                    new TopRatingRecord(
                                        reader.GetInt32(column_topRank),
                                        reader.GetString(column_playerName),
                                        reader.GetInt32(column_rating)));
                            }

                            return topList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top players by rating (statPeriodId:{statPeriodId}, top:{top}).", statPeriodId, top);
                throw;
            }
        }

        public async Task<List<TopAvgRatingRecord>> GetTopTeamVersusPlayersByAvgRating(long statPeriodId, int top, int minGamesPlayed, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_top_versus_players_by_avg_rating($1,$2,$3);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, statPeriodId);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, top);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, minGamesPlayed);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_topRank = reader.GetOrdinal("top_rank");
                            int column_playerName = reader.GetOrdinal("player_name");
                            int column_avgRating = reader.GetOrdinal("avg_rating");

                            List<TopAvgRatingRecord> topList = new(top);

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                topList.Add(
                                    new TopAvgRatingRecord(
                                        reader.GetInt32(column_topRank),
                                        reader.GetString(column_playerName),
                                        reader.GetFloat(column_avgRating)));
                            }

                            return topList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top versus game players by avg rating (statPeriodId:{statPeriodId}, top:{top}).", statPeriodId, top);
                throw;
            }
        }

        public async Task<List<TopKillsPerMinuteRecord>> GetTopTeamVersusPlayersByKillsPerMinute(long statPeriodId, int top, int minGamesPlayed, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_top_versus_players_by_kills_per_minute($1,$2);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, statPeriodId);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, top);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, minGamesPlayed);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_topRank = reader.GetOrdinal("top_rank");
                            int column_playerName = reader.GetOrdinal("player_name");
                            int column_killsPerMinute = reader.GetOrdinal("kills_per_minute");

                            List<TopKillsPerMinuteRecord> topList = new(top);

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                topList.Add(
                                    new TopKillsPerMinuteRecord(
                                        reader.GetInt32(column_topRank),
                                        reader.GetString(column_playerName),
                                        reader.GetFloat(column_killsPerMinute)));
                            }

                            return topList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top versus game players by kills per minute (statPeriodId:{statPeriodId}, top:{top}).", statPeriodId, top);
                throw;
            }
        }

        public async Task<List<TeamVersusPeriodStats>> GetTeamVersusPeriodStats(string playerName, List<StatPeriod> statPeriodList, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(playerName);
            ArgumentNullException.ThrowIfNull(statPeriodList);

            if (statPeriodList.Count == 0)
                throw new ArgumentException("At least one period is required.", nameof(statPeriodList));

            List<long> statPeriodIdList = new(statPeriodList.Count);
            foreach (StatPeriod statPeriod in statPeriodList)
                statPeriodIdList.Add(statPeriod.StatPeriodId);

            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_player_versus_period_stats($1,$2);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Varchar, playerName);
                        command.Parameters.AddWithValue(NpgsqlDbType.Array | NpgsqlDbType.Bigint, statPeriodIdList);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_statPeriodId = reader.GetOrdinal("stat_period_id");
                            int column_periodRank = reader.GetOrdinal("period_rank");
                            int column_rating = reader.GetOrdinal("rating");
                            int column_gamesPlayed = reader.GetOrdinal("games_played");
                            int column_playDuration = reader.GetOrdinal("play_duration");
                            int column_wins = reader.GetOrdinal("wins");
                            int column_losses = reader.GetOrdinal("losses");
                            int column_lagOuts = reader.GetOrdinal("lag_outs");
                            int column_kills = reader.GetOrdinal("kills");
                            int column_deaths = reader.GetOrdinal("deaths");
                            int column_knockouts = reader.GetOrdinal("knockouts");
                            int column_teamKills = reader.GetOrdinal("team_kills");
                            int column_soloKills = reader.GetOrdinal("solo_kills");
                            int column_assists = reader.GetOrdinal("assists");
                            int column_forcedReps = reader.GetOrdinal("forced_reps");
                            int column_gunDamageDealt = reader.GetOrdinal("gun_damage_dealt");
                            int column_bombDamageDealt = reader.GetOrdinal("bomb_damage_dealt");
                            int column_teamDamageDealt = reader.GetOrdinal("team_damage_dealt");
                            int column_gunDamageTaken = reader.GetOrdinal("gun_damage_taken");
                            int column_bombDamageTaken = reader.GetOrdinal("bomb_damage_taken");
                            int column_teamDamageTaken = reader.GetOrdinal("team_damage_taken");
                            int column_selfDamage = reader.GetOrdinal("self_damage");
                            int column_killDamage = reader.GetOrdinal("kill_damage");
                            int column_teamKillDamage = reader.GetOrdinal("team_kill_damage");
                            int column_forcedRepDamage = reader.GetOrdinal("forced_rep_damage");
                            int column_bulletFireCount = reader.GetOrdinal("bullet_fire_count");
                            int column_bombFireCount = reader.GetOrdinal("bomb_fire_count");
                            int column_mineFireCount = reader.GetOrdinal("mine_fire_count");
                            int column_bulletHitCount = reader.GetOrdinal("bullet_hit_count");
                            int column_bombHitCount = reader.GetOrdinal("bomb_hit_count");
                            int column_mineHitCount = reader.GetOrdinal("mine_hit_count");
                            int column_firstOutRegular = reader.GetOrdinal("first_out_regular");
                            int column_firstOutCritical = reader.GetOrdinal("first_out_critical");
                            int column_wastedEnergy = reader.GetOrdinal("wasted_energy");
                            int column_wastedRepel = reader.GetOrdinal("wasted_repel");
                            int column_wastedRocket = reader.GetOrdinal("wasted_rocket");
                            int column_wastedThor = reader.GetOrdinal("wasted_thor");
                            int column_wastedBurst = reader.GetOrdinal("wasted_burst");
                            int column_wastedDecoy = reader.GetOrdinal("wasted_decoy");
                            int column_wastedPortal = reader.GetOrdinal("wasted_portal");
                            int column_wastedBrick = reader.GetOrdinal("wasted_brick");
                            int column_enemyDistanceSum = reader.GetOrdinal("enemy_distance_sum");
                            int column_enemyDistanceSamples = reader.GetOrdinal("enemy_distance_samples");
                            int column_teamDistanceSum = reader.GetOrdinal("team_distance_sum");
                            int column_teamDistanceSamples = reader.GetOrdinal("team_distance_samples");
                            int column_ratingAvg = reader.GetOrdinal("rating_avg");

                            List<TeamVersusPeriodStats> periodStatsList = [];

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                long statPeriodId = reader.GetInt64(column_statPeriodId);
                                StatPeriod? statPeriod = null;
                                foreach (StatPeriod period in statPeriodList)
                                {
                                    if (period.StatPeriodId == statPeriodId)
                                    {
                                        statPeriod = period;
                                        break;
                                    }
                                }

                                if (statPeriod is null)
                                    continue;

                                periodStatsList.Add(
                                    new TeamVersusPeriodStats()
                                    {
                                        StatPeriod = statPeriod.Value,
                                        Rank = reader.IsDBNull(column_periodRank) ? null : reader.GetInt32(column_periodRank),
                                        Rating = reader.IsDBNull(column_rating) ? null : reader.GetInt32(column_rating),
                                        Games = reader.GetInt64(column_gamesPlayed),
                                        Wins = reader.GetInt64(column_wins),
                                        Losses = reader.GetInt64(column_losses),
                                        FirstOutRegular = reader.GetInt64(column_firstOutRegular),
                                        FirstOutCritical = reader.GetInt64(column_firstOutCritical),
                                        PlayDuration = reader.GetTimeSpan(column_playDuration),
                                        LagOuts = reader.GetInt64(column_lagOuts),
                                        Kills = reader.GetInt64(column_kills),
                                        Deaths = reader.GetInt64(column_deaths),
                                        Knockouts = reader.GetInt64(column_knockouts),
                                        TeamKills = reader.GetInt64(column_teamKills),
                                        SoloKills = reader.GetInt64(column_soloKills),
                                        Assists = reader.GetInt64(column_assists),
                                        ForcedReps = reader.GetInt64(column_forcedReps),
                                        GunDamageDealt = reader.GetInt64(column_gunDamageDealt),
                                        BombDamageDealt = reader.GetInt64(column_bombDamageDealt),
                                        TeamDamageDealt = reader.GetInt64(column_teamDamageDealt),
                                        GunDamageTaken = reader.GetInt64(column_gunDamageTaken),
                                        BombDamageTaken = reader.GetInt64(column_bombDamageTaken),
                                        TeamDamageTaken = reader.GetInt64(column_teamDamageTaken),
                                        SelfDamage = reader.GetInt64(column_selfDamage),
                                        KillDamage = reader.GetInt64(column_killDamage),
                                        TeamKillDamage = reader.GetInt64(column_teamKillDamage),
                                        ForcedRepDamage = reader.GetInt64(column_forcedRepDamage),
                                        BulletFireCount = reader.GetInt64(column_bulletFireCount),
                                        BombFireCount = reader.GetInt64(column_bombFireCount),
                                        MineFireCount = reader.GetInt64(column_mineFireCount),
                                        BulletHitCount = reader.GetInt64(column_bulletHitCount),
                                        BombHitCount = reader.GetInt64(column_bombHitCount),
                                        MineHitCount = reader.GetInt64(column_mineHitCount),
                                        WastedEnergy = reader.GetInt64(column_wastedEnergy),
                                        WastedRepel = reader.GetInt64(column_wastedRepel),
                                        WastedRocket = reader.GetInt64(column_wastedRocket),
                                        WastedThor = reader.GetInt64(column_wastedThor),
                                        WastedBurst = reader.GetInt64(column_wastedBurst),
                                        WastedDecoy = reader.GetInt64(column_wastedDecoy),
                                        WastedPortal = reader.GetInt64(column_wastedPortal),
                                        WastedBrick = reader.GetInt64(column_wastedBrick),
                                        EnemyDistanceSum = reader.IsDBNull(column_enemyDistanceSum) ? null : reader.GetInt64(column_enemyDistanceSum),
                                        EnemyDistanceSamples = reader.IsDBNull(column_enemyDistanceSamples) ? null : reader.GetInt64(column_enemyDistanceSamples),
                                        TeamDistanceSum = reader.IsDBNull(column_teamDistanceSum) ? null : reader.GetInt64(column_teamDistanceSum),
                                        TeamDistanceSamples = reader.IsDBNull(column_teamDistanceSamples) ? null : reader.GetInt64(column_teamDistanceSamples),
                                        RatingAverage = reader.GetFloat(column_ratingAvg),
                                    });
                            }

                            return periodStatsList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team versus period stats (playerName:{playerName}, statPeriodIds:{statPeriodIds}).", playerName, string.Join(',', statPeriodIdList));
                throw;
            }
        }

        public async Task<List<TeamVersusGameStats>> GetTeamVersusGameStats(string playerName, long statPeriodId, int limit, int offset, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_player_versus_game_stats($1,$2,$3,$4);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Varchar, playerName);
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, statPeriodId);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, limit);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, offset);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_gameId = reader.GetOrdinal("game_id");
                            int column_timePlayed = reader.GetOrdinal("time_played");
                            int column_score = reader.GetOrdinal("score");
                            int column_result = reader.GetOrdinal("result");
                            int column_playDuration = reader.GetOrdinal("play_duration");
                            int column_shipMask = reader.GetOrdinal("ship_mask");
                            int column_lagOuts = reader.GetOrdinal("lag_outs");
                            int column_kills = reader.GetOrdinal("kills");
                            int column_deaths = reader.GetOrdinal("deaths");
                            int column_knockouts = reader.GetOrdinal("knockouts");
                            int column_teamKills = reader.GetOrdinal("team_kills");
                            int column_soloKills = reader.GetOrdinal("solo_kills");
                            int column_assists = reader.GetOrdinal("assists");
                            int column_forcedReps = reader.GetOrdinal("forced_reps");
                            int column_gunDamageDealt = reader.GetOrdinal("gun_damage_dealt");
                            int column_bombDamageDealt = reader.GetOrdinal("bomb_damage_dealt");
                            int column_teamDamageDealt = reader.GetOrdinal("team_damage_dealt");
                            int column_gunDamageTaken = reader.GetOrdinal("gun_damage_taken");
                            int column_bombDamageTaken = reader.GetOrdinal("bomb_damage_taken");
                            int column_teamDamageTaken = reader.GetOrdinal("team_damage_taken");
                            int column_selfDamage = reader.GetOrdinal("self_damage");
                            int column_killDamage = reader.GetOrdinal("kill_damage");
                            int column_teamKillDamage = reader.GetOrdinal("team_kill_damage");
                            int column_forcedRepDamage = reader.GetOrdinal("forced_rep_damage");
                            int column_bulletFireCount = reader.GetOrdinal("bullet_fire_count");
                            int column_bombFireCount = reader.GetOrdinal("bomb_fire_count");
                            int column_mineFireCount = reader.GetOrdinal("mine_fire_count");
                            int column_bulletHitCount = reader.GetOrdinal("bullet_hit_count");
                            int column_bombHitCount = reader.GetOrdinal("bomb_hit_count");
                            int column_mineHitCount = reader.GetOrdinal("mine_hit_count");
                            int column_firstOut = reader.GetOrdinal("first_out");
                            int column_wastedEnergy = reader.GetOrdinal("wasted_energy");
                            int column_wastedRepel = reader.GetOrdinal("wasted_repel");
                            int column_wastedRocket = reader.GetOrdinal("wasted_rocket");
                            int column_wastedThor = reader.GetOrdinal("wasted_thor");
                            int column_wastedBurst = reader.GetOrdinal("wasted_burst");
                            int column_wastedDecoy = reader.GetOrdinal("wasted_decoy");
                            int column_wastedPortal = reader.GetOrdinal("wasted_portal");
                            int column_wastedBrick = reader.GetOrdinal("wasted_brick");
                            int column_ratingChange = reader.GetOrdinal("rating_change");
                            int column_enemyDistanceSum = reader.GetOrdinal("enemy_distance_sum");
                            int column_enemyDistanceSamples = reader.GetOrdinal("enemy_distance_samples");
                            int column_teamDistanceSum = reader.GetOrdinal("team_distance_sum");
                            int column_teamDistanceSamples = reader.GetOrdinal("team_distance_samples");

                            List<TeamVersusGameStats> statsList = new(limit);

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                statsList.Add(new TeamVersusGameStats()
                                {
                                    GameId = reader.GetInt64(column_gameId),
                                    TimePlayed = reader.GetFieldValue<NpgsqlRange<DateTime>>(column_timePlayed),
                                    Score = reader.GetFieldValue<int[]>(column_score),
                                    Result = (GameResult)reader.GetInt32(column_result),
                                    PlayDuration = reader.GetTimeSpan(column_playDuration),
                                    ShipMask = (ShipMask)reader.GetInt16(column_shipMask),
                                    LagOuts = reader.GetInt16(column_lagOuts),
                                    Kills = reader.GetInt16(column_kills),
                                    Deaths = reader.GetInt16(column_deaths),
                                    Knockouts = reader.GetInt16(column_knockouts),
                                    TeamKills = reader.GetInt16(column_teamKills),
                                    SoloKills = reader.GetInt16(column_soloKills),
                                    Assists = reader.GetInt16(column_assists),
                                    ForcedReps = reader.GetInt16(column_forcedReps),
                                    GunDamageDealt = reader.GetInt32(column_gunDamageDealt),
                                    BombDamageDealt = reader.GetInt32(column_bombDamageDealt),
                                    TeamDamageDealt = reader.GetInt32(column_teamDamageDealt),
                                    GunDamageTaken = reader.GetInt32(column_gunDamageTaken),
                                    BombDamageTaken = reader.GetInt32(column_bombDamageTaken),
                                    TeamDamageTaken = reader.GetInt32(column_teamDamageTaken),
                                    SelfDamage = reader.GetInt32(column_selfDamage),
                                    KillDamage = reader.GetInt32(column_killDamage),
                                    TeamKillDamage = reader.GetInt32(column_teamKillDamage),
                                    ForcedRepDamage = reader.GetInt32(column_forcedRepDamage),
                                    BulletFireCount = reader.GetInt32(column_bulletFireCount),
                                    BombFireCount = reader.GetInt32(column_bombFireCount),
                                    MineFireCount = reader.GetInt32(column_mineFireCount),
                                    BulletHitCount = reader.GetInt32(column_bulletHitCount),
                                    BombHitCount = reader.GetInt32(column_bombHitCount),
                                    MineHitCount = reader.GetInt32(column_mineHitCount),
                                    FirstOut = (FirstOut)reader.GetInt16(column_firstOut),
                                    WastedEnergy = reader.GetInt32(column_wastedEnergy),
                                    WastedRepel = reader.GetInt16(column_wastedRepel),
                                    WastedRocket = reader.GetInt16(column_wastedRocket),
                                    WastedThor = reader.GetInt16(column_wastedThor),
                                    WastedBurst = reader.GetInt16(column_wastedBurst),
                                    WastedDecoy = reader.GetInt16(column_wastedDecoy),
                                    WastedPortal = reader.GetInt16(column_wastedPortal),
                                    WastedBrick = reader.GetInt16(column_wastedBrick),
                                    RatingChange = reader.GetInt32(column_ratingChange),
                                    EnemyDistanceSum = reader.IsDBNull(column_enemyDistanceSum) ? null : reader.GetInt64(column_enemyDistanceSum),
                                    EnemyDistanceSamples = reader.IsDBNull(column_enemyDistanceSamples) ? null : reader.GetInt32(column_enemyDistanceSamples),
                                    TeamDistanceSum = reader.IsDBNull(column_teamDistanceSum) ? null : reader.GetInt64(column_teamDistanceSum),
                                    TeamDistanceSamples = reader.IsDBNull(column_teamDistanceSamples) ? null : reader.GetInt32(column_teamDistanceSamples),
                                });
                            }

                            return statsList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team versus game stats (playerName:{playerName} statPeriodId:{statPeriodId}, limit:{limit}, offset:{offset}).", playerName, statPeriodId, limit, offset);
                throw;
            }
        }

        public async Task<List<TeamVersusShipStats>> GetTeamVersusShipStats(string playerName, long statPeriodId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_player_versus_ship_stats($1,$2);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Varchar, playerName);
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, statPeriodId);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_shipType = reader.GetOrdinal("ship_type");
                            int column_gameUseCount = reader.GetOrdinal("game_use_count");
                            int column_useDuration = reader.GetOrdinal("use_duration");
                            int column_kills = reader.GetOrdinal("kills");
                            int column_deaths = reader.GetOrdinal("deaths");
                            int column_knockouts = reader.GetOrdinal("knockouts");
                            int column_soloKills = reader.GetOrdinal("solo_kills");

                            List<TeamVersusShipStats> shipStatsList = [];
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                shipStatsList.Add(
                                    new TeamVersusShipStats()
                                    {
                                        ShipType = (ShipType)reader.GetInt16(column_shipType),
                                        GameUseCount = reader.GetInt32(column_gameUseCount),
                                        UseDuration = reader.GetTimeSpan(column_useDuration),
                                        Kills = reader.GetInt64(column_kills),
                                        Deaths = reader.GetInt64(column_deaths),
                                        Knockouts = reader.GetInt64(column_knockouts),
                                        SoloKills = reader.GetInt64(column_soloKills),
                                    });
                            }
                            return shipStatsList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team versus ship stats (playerName:{playerName}, statPeriodId:{statPeriodId}).", playerName, statPeriodId);
                throw;
            }
        }

        public async Task<List<KillStats>> GetTeamVersusKillStats(string playerName, long statPeriodId, int limit, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select * from ss.get_player_versus_kill_stats($1,$2,$3);", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Varchar, playerName);
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, statPeriodId);
                        command.Parameters.AddWithValue(NpgsqlDbType.Integer, limit);

                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            int column_playerName = reader.GetOrdinal("player_name");
                            int column_kills = reader.GetOrdinal("kills");
                            int column_deaths = reader.GetOrdinal("deaths");

                            List<KillStats> killStatsList = [];
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                killStatsList.Add(
                                    new KillStats()
                                    {
                                        PlayerName = reader.GetString(column_playerName),
                                        Kills = reader.GetInt64(column_kills),
                                        Deaths = reader.GetInt64(column_deaths),
                                    });
                            }
                            return killStatsList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team versus kill stats (playerName:{playerName}, statPeriodId:{statPeriodId}).", playerName, statPeriodId);
                throw;
            }
        }

        public async Task RefreshTeamVersusPlayerStats(long statPeriodId, CancellationToken cancellationToken)
        {
            try
            {
                NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using (connection.ConfigureAwait(false))
                {
                    NpgsqlCommand command = new("select ss.refresh_player_versus_stats($1)", connection);
                    await using (command.ConfigureAwait(false))
                    {
                        command.Parameters.Add(new NpgsqlParameter<long> { TypedValue = statPeriodId });
                        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing player versus stats. (stat_period_id: {stat_period_id})", statPeriodId);
                throw;
            }
        }
    }
}

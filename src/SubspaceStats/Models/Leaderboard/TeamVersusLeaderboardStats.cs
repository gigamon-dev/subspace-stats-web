namespace SubspaceStats.Models.Leaderboard
{
    public class TeamVersusLeaderboardStats
    {
        public required long RatingRank { get; init; }
        public required string PlayerName { get; init; }
        public required string? SquadName { get; init; }
        public required int Rating { get; init; }
        public required float RatingAverage { get; init; }
        public required long GamesPlayed { get; init; }
        public required TimeSpan PlayDuration { get; init; }
        public required long Wins { get; init; }
        public required long Losses { get; init; }
        public required long Kills { get; init; }
        public required long Deaths { get; init; }
        public required long DamageDealt { get; init; }
        public required long DamageTaken { get; init; }
        public required long KillDamage { get; init; }
        public required long ForcedReps { get; init; }
        public required long ForcedRepDamage { get; init; }
        public required long Assists { get; init; }
        public required long WastedEnergy { get; init; }
        public required long FirstOut { get; init; }
    }
}

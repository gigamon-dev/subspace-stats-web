namespace SubspaceStats.Models.Player
{
    public class TeamVersusPeriodStats
    {
        public required StatPeriod StatPeriod { get; init; }
        public required int? Rank { get; init; }

        /// <summary>
        /// The current rating of the player for the <see cref="StatPeriod"/>.
        /// </summary>
        /// <remarks>
        /// A player's rating for a stat period starts at an initial amount (ss.stat_tracking.initial_rating).
        /// Also, it cannot fall below a certain amount (ss.stat_tracking.minimum_rating).
        /// </remarks>
        public int? Rating { get; init; }
        public required long Games { get; init; }
        public required long Wins { get; init; }
        public required long Losses { get; init; }
        public required long FirstOutRegular { get; init; }
        public required long FirstOutCritical { get; init; }
        public required TimeSpan PlayDuration { get; init; }
        public required long LagOuts { get; init; }
        public required long Kills { get; init; }
        public required long Deaths { get; init; }
        public required long Knockouts { get; init; }
        public required long TeamKills { get; init; }
        public required long SoloKills { get; init; }
        public required long Assists { get; init; }
        public required long ForcedReps { get; init; }
        public required long GunDamageDealt { get; init; }
        public required long BombDamageDealt { get; init; }
        public required long TeamDamageDealt { get; init; }
        public required long GunDamageTaken { get; init; }
        public required long BombDamageTaken { get; init; }
        public required long TeamDamageTaken { get; init; }
        public required long SelfDamage { get; init; }
        public required long KillDamage { get; init; }
        public required long TeamKillDamage { get; init; }
        public required long ForcedRepDamage { get; init; }
        public required long BulletFireCount { get; init; }
        public required long BombFireCount { get; init; }
        public required long MineFireCount { get; init; }
        public required long BulletHitCount { get; init; }
        public required long BombHitCount { get; init; }
        public required long MineHitCount { get; init; }
        public required long WastedEnergy { get; init; }
        public required long WastedRepel { get; init; }
        public required long WastedRocket { get; init; }
        public required long WastedThor { get; init; }
        public required long WastedBurst { get; init; }
        public required long WastedDecoy { get; init; }
        public required long WastedPortal { get; init; }
        public required long WastedBrick { get; init; }
        public required long? EnemyDistanceSum { get; init; }
        public required long? EnemyDistanceSamples { get; init; }
        public required long? TeamDistanceSum { get; init; }
        public required long? TeamDistanceSamples { get; init; }
        public required float RatingAverage { get; init; }
    }
}

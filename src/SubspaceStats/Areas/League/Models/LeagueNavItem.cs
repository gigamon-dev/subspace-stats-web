using System.Text.Json.Serialization;

namespace SubspaceStats.Areas.League.Models
{
    public class LeagueNavItem
    {
        [JsonPropertyName("league_id")]
        public required long LeagueId { get; init; }

        [JsonPropertyName("league_name")]
        public required string LeagueName { get; init; }

        [JsonPropertyName("seasons")]
        public required List<SeasonNavItem> Seasons { get; init; }
    }

    public class SeasonNavItem
    {
        [JsonPropertyName("season_id")]
        public required long SeasonId { get; init; }

        [JsonPropertyName("season_name")]
        public required string SeasonName { get; init; }
    }

    [JsonSerializable(typeof(List<LeagueNavItem>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal partial class SeasonNavSourceGenerationContext : JsonSerializerContext
    {
    }
}

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SubspaceStats.Areas.League.Models.Season.Round
{
    public class SeasonRound
    {
        [Display(Name = "Season ID")]
        public required long SeasonId { get; set; }

        [Display(Name = "Round Number")]
        [Remote("ValidateSeasonRound", "SeasonRound", "League", AdditionalFields = nameof(SeasonId))]
        public required int RoundNumber { get; set; }

        [Display(Name = "Round Name")]
        [Required]
        [StringLength(128, MinimumLength = 1)]
        public required string RoundName { get; set; }

        [Display(Name = "Round Description")]
        [StringLength(8192)]
        public string? RoundDescription { get; set; }
    }
}

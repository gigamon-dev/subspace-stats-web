using System.Diagnostics.CodeAnalysis;

namespace SubspaceStats.Areas.League.Models
{
    public class LeagueSeasonChooserViewModel
    {
        public LeagueSeasonChooserViewModel(List<LeagueNavItem> leaguesWithSeasons, long? selectedLeagueId, long? selectedSeasonId)
        {
            LeaguesWithSeasons = leaguesWithSeasons;

            foreach (LeagueNavItem league in LeaguesWithSeasons)
            {
                if (selectedSeasonId is not null)
                {
                    if (TryGetSelectedSeason(league, selectedSeasonId.Value, out SeasonNavItem? season))
                    {
                        SelectedLeague = league;
                        SelectedSeason = season;
                        break;
                    }
                }
                else if (selectedLeagueId is not null && league.LeagueId == selectedLeagueId)
                {
                    SelectedLeague = league;
                }
            }

            static bool TryGetSelectedSeason(LeagueNavItem league, long selectedSeasonId, [MaybeNullWhen(false)]out SeasonNavItem selectedSeason)
            {
                foreach (var season in league.Seasons)
                {
                    if (season.SeasonId == selectedSeasonId)
                    {
                        selectedSeason = season;
                        return true;
                    }
                }

                selectedSeason = null;
                return false;
            }
        }

        public List<LeagueNavItem> LeaguesWithSeasons { get; }
        public LeagueNavItem? SelectedLeague { get; }
        public SeasonNavItem? SelectedSeason { get; }
    }
}

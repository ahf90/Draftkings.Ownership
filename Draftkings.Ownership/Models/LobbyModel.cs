using System.Collections.Generic;
using System.Data.Entity;

namespace Draftkings.Ownership.Models
{
    public class LobbyRoot
    {
        public int id { get; set; }
        public object SelectedSport { get; set; }
        public List<ContestJson> Contests { get; set; }
        public List<ContestGroupJson> DraftGroups { get; set; }
        public List<string> MarketingOffers { get; set; }
        public object DirectChallengeModal { get; set; }
        public object DepositTransaction { get; set; }
        public bool ShowRafLink { get; set; }
        public bool ShowRafModal { get; set; }
        public object PrizeRedemptionModel { get; set; }
        public bool PrizeRedemptionPop { get; set; }
        public bool UseRaptorHeadToHead { get; set; }
        public object SportMenuItems { get; set; }
        public object UserGeoLocation { get; set; }
    }

}
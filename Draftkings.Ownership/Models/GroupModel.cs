using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Draftkings.Ownership.Models
{
    public class ContestGroupJson
    {
        public int DraftGroupId { get; set; }
        public int ContestTypeId { get; set; }
        public string StartDate { get; set; }
        public DateTime ContestStartDate { get; set; }
        public string StartDateEst { get; set; }
        public string Sport { get; set; }
        public DateTime LastGameStart { get; set; }
        public int GameCount { get; set; }
        public string ContestStartTimeSuffix { get; set; }
        public int ContestStartTimeType { get; set; }
    }
}
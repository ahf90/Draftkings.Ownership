using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Draftkings.Ownership.Models
{
    public class Attr
    {
        public string IsGuaranteed { get; set; }
        public string IsPrivate { get; set; }
        public string IsStarred { get; set; }
        public string IsQualifier { get; set; }
        public string IsDoubleUp { get; set; }
        public string IsFiftyfifty { get; set; }
        public string IsHeadliner { get; set; }
        public string IsBeginner { get; set; }
    }

    public class Pd
    {
        public string Cash { get; set; }
        public string Prize { get; set; }
        public string Ticket { get; set; }
    }

    public class ContestJson
    {
        public int ContestId { get; set; }
        public int DraftGroupId { get; set; }
        public int id { get; set; }
        public int uc { get; set; }
        public int ec { get; set; }
        public int mec { get; set; }
        public int fpp { get; set; }
        public int s { get; set; }
        public string n { get; set; }
        public Attr attr { get; set; }
        public int nt { get; set; }
        public int m { get; set; }
        public float a { get; set; }
        public float po { get; set; }
        public Pd pd { get; set; }
        public bool tix { get; set; }
        public string sdstring { get; set; }
        public double EpochTime { get; set; }
        public string sd { get; set; }
        public int tmpl { get; set; }
        public int pt { get; set; }
        public int so { get; set; }
        public bool fwt { get; set; }
        public bool isOwner { get; set; }
        public int startTimeType { get; set; }
        public int dg { get; set; }
        public int ulc { get; set; }
        public int cs { get; set; }
        public object ssd { get; set; }
        public double dgpo { get; set; }
        public int cso { get; set; }
        public bool rl { get; set; }
        public int rlc { get; set; }
        public int rll { get; set; }
        public bool sa { get; set; }
        public bool ScrapeReady { get; set; }
        public bool IsOwnershipScraped { get; set; }
        public int UserIdScrapes { get; set; }
    }


}
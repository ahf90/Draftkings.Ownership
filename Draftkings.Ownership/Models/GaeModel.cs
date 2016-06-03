using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Draftkings.Ownership.Models
{
    public class PdJsonGae
    {
        public string Cash { get; set; }
    }

    public class AttrJsonGae
    {
        public string IsGuaranteed { get; set; }
        public string IsStarred { get; set; }
    }

    public class GameJsonGae
    {
        public int tmpl { get; set; }
        public int ec { get; set; }
        public bool isOwner { get; set; }
        public int cs { get; set; }
        public int id { get; set; }
        public int cso { get; set; }
        public bool tix { get; set; }
        public int pt { get; set; }
        public int startTimeType { get; set; }
        public int fpp { get; set; }
        public PdJsonGae pd { get; set; }
        public bool rl { get; set; }
        public int nt { get; set; }
        public int po { get; set; }
        public int rll { get; set; }
        public int mec { get; set; }
        public int dg { get; set; }
        public int rlc { get; set; }
        public double dgpo { get; set; }
        public object ssd { get; set; }
        public double a { get; set; }
        public AttrJsonGae attr { get; set; }
        public int m { get; set; }
        public string n { get; set; }
        public string s { get; set; }
        public int so { get; set; }
        public string sdstring { get; set; }
        public bool fwt { get; set; }
        public int ulc { get; set; }
        public bool sa { get; set; }
        public int uc { get; set; }
        public DateTime sd { get; set; }
    }

    public class ContestJsonGae
    {
        public object standings { get; set; }
        public object entryCount { get; set; }
        public object slateSize { get; set; }
        public bool beginner { get; set; }
        public string title { get; set; }
        public bool calculated { get; set; }
        public int lastGameTime { get; set; }
        public string sport { get; set; }
        public object players { get; set; }
        public GameJsonGae game { get; set; }
        public string contestType { get; set; }
        public double entryFee { get; set; }
        public bool multi { get; set; }
        public int gameId { get; set; }
        public bool scraped { get; set; }
        public int start { get; set; }
        public int size { get; set; }
    }
}
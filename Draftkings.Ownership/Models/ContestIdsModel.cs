using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Draftkings.Ownership.Models
{
    public class GameCenterRoot
    {
        public List<ContestEntryJson> ContestEntries { get; set; }

    }
    public class ContestEntryJson
    {
        public int uc { get; set; }
        public int u { get; set; }
        public string un { get; set; }
        public string t { get; set; }
        public int r { get; set; }
        public int pmr { get; set; }
        public float pts { get; set; }
        public int ContestId { get; set; }
    }
}
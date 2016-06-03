using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Draftkings.Ownership.Models
{
    public class RequestObject
    {
        public List<double> idList { get; set; }
        public long reqTs { get; set; }
        public int contestId { get; set; }
        public int draftGroupId { get; set; }
    }
}
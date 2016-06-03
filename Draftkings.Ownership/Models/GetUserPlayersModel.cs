using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Draftkings.Ownership.Models
{
    public class UserPlayerDataJson
    {
        public int pid { get; set; }
        public float pd { get; set; }
        public int ContestId { get; set; }
    }

    public class UserPlayerDataRootJson
    {
        public int status { get; set; }
        public long reqTs { get; set; }
        public Dictionary<string, List<UserPlayerDataJson>> data { get; set; }
    }
}
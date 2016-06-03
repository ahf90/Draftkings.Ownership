using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Draftkings.Ownership.Models
{
    public class PlayerJson
    {
        public int pid { get; set; }
        public int pcode { get; set; }
        public int tsid { get; set; }
        public string fn { get; set; }
        public string ln { get; set; }
        public string fnu { get; set; }
        public string lnu { get; set; }
        public string pn { get; set; }
        public object dgst { get; set; }
        public int tid { get; set; }
        public int htid { get; set; }
        public int atid { get; set; }
        public string htabbr { get; set; }
        public string atabbr { get; set; }
        public int posid { get; set; }
        public object slo { get; set; }
        public bool IsDisabledFromDrafting { get; set; }
        public List<object> ExceptionalMessages { get; set; }
        public int s { get; set; }
        public string ppg { get; set; }
        public int or { get; set; }
        public bool swp { get; set; }
        public int pp { get; set; }
        public string i { get; set; }
        public int news { get; set; }
    }

    public class Fixture
    {
        public string ht { get; set; }
        public int htid { get; set; }
        public string at { get; set; }
        public int atid { get; set; }
        public string tz { get; set; }
        public Double tzEpoch { get; set; }
        public DateTime GameStart { get; set; }
        public string wthr { get; set; }
        public int dh { get; set; }
        public int s { get; set; }
        public int status { get; set; }
        public bool lrdy { get; set; }
    }
    public class SalaryRoot
    {
        public List<PlayerJson> playerList { get; set; }
        public Dictionary<string, Fixture> teamList { get; set; }
    }

}
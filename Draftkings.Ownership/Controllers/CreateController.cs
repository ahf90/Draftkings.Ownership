using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Mvc;
using System.IO;
using System.Net;
using System.Threading;
using Draftkings.Ownership.Models;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using RestSharp;
using Hangfire;

namespace Draftkings.Ownership.Controllers
{
    public class CreateController : Controller
    {
        private FantasyContestsDBContextDk Database = new FantasyContestsDBContextDk();
        [Queue("playercreate")]
        public void ContestPlayers(int ContestId)
        {

            bool PlayerExists;
            Contest CurrentContest = Database.Contests.Find(ContestId);

            if (CurrentContest == null) { return; }

            int DraftGroupId = CurrentContest.ContestGroupId;
            var DraftGroupPlayerList = (from e in Database.DraftGroupPlayers
                                        where e.DraftGroupId == DraftGroupId
                                        select e.PlayerId).ToList();

            foreach (int ListPlayerId in DraftGroupPlayerList)
            {
                PlayerExists = Database.ContestPlayers.Any(pl => pl.ContestId == ContestId && pl.PlayerId == ListPlayerId);
                if (PlayerExists == false)
                {
                    ContestPlayer newPlayer = new ContestPlayer();
                    newPlayer.PlayerId = ListPlayerId;
                    newPlayer.ContestId = ContestId;
                    Database.ContestPlayers.Add(newPlayer);
                    Database.SaveChanges();
                }
                else
                {
                    Debug.WriteLine("Already exists");
                    return;
                }
            }
        }
    }
}
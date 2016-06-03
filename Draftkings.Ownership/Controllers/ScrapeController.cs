using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Draftkings.Ownership.Models;
using RestSharp;
using System.Diagnostics;
using Hangfire;

namespace Draftkings.Ownership.Controllers
{
    public class ScrapeController : Controller
    {
        private FantasyContestsDBContextDk db = new FantasyContestsDBContextDk();
        private CreateController CreateControllerInstance = new CreateController();
        private static readonly List<string> LoginList = new List<string> { "num2sharkfin", "num2sharkfin", "num2sharkfin", "num2sharkfin", "num2sharkfin", "num2sharkfin" };

        [Queue("entryids")]
        public void ContestGroupFetchEntryIds(int ContestGroupId)
        {
            var ContestList = (from e in db.ScrapeStatuses
                               where e.ContestGroupId == ContestGroupId && e.FinalEntryIdScrape == false
                               select e.ContestId).ToList();

            var DkClient = new RestClient("https://www.draftkings.com");
            IRestResponse DkResponse = Login(DkClient);

            RestRequest LoginRequestTwo = new RestRequest("contest/gamecenter/", Method.GET);
            foreach (var cookie in DkResponse.Cookies)
            {
                LoginRequestTwo.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
            }

            foreach (int ContestId in ContestList)
            {
                FetchContestEntryIds(LoginRequestTwo, DkClient, ContestId);
            }
        }
        public void StoreEntryIds(List<ContestEntryJson> IdList, int ContestId)
        {
            bool ContestEntryExists;

            foreach (ContestEntryJson Entry in IdList)
            {
                ContestEntryExists = db.ContestEntries.Any(ce => ce.EntryId == Entry.uc);
                if (ContestEntryExists == false)
                {
                    ContestEntry NewContestEntry = new ContestEntry();
                    NewContestEntry.EntryId = Entry.uc;
                    NewContestEntry.ContestId = ContestId;

                    string[] RankSplitString = Entry.t.Split('/');
                    string RankNumber = Regex.Replace(RankSplitString[0], "[^0-9]", "");
                    NewContestEntry.Rank = Int32.Parse(RankNumber);

                    NewContestEntry.Score = Entry.pts;
                    NewContestEntry.Username = Entry.un;
                    db.ContestEntries.Add(NewContestEntry);
                    db.SaveChanges();
                }
            }
        }
        public void FetchContestEntryIds(RestRequest LoginRequestTwo, RestClient DkClient, int ContestId)
        {
            ContestScrapeStatus CurrentContestStatus = db.ScrapeStatuses
                                                           .Where(i => i.ContestId == ContestId)
                                                           .SingleOrDefault();

            Contest CurrentContest = db.Contests
                                    .Where(i => i.ContestId == ContestId)
                                    .SingleOrDefault();

            Thread.Sleep(6000);

            LoginRequestTwo.Resource = "contest/gamecenter/" + ContestId;
            IRestResponse entryResponse = DkClient.Execute(LoginRequestTwo);

            string Source = entryResponse.Content;
            int IdStartIndex = Regex.Match(Source, "var teams ").Index;
            IdStartIndex = IdStartIndex + 12;

            Source = Source.Substring(IdStartIndex, Source.Length - IdStartIndex);
            int SemiColonIndex = Regex.Match(Source, ";").Index;
            Source = Source.Substring(0, SemiColonIndex);
            List<ContestEntryJson> IdList = JsonConvert.DeserializeObject<List<ContestEntryJson>>(Source);

            StoreEntryIds(IdList, ContestId);

            if (CurrentContestStatus.InitialEntryIdScrape == false)
            {
                CurrentContestStatus.InitialEntryIdScrape = true;
                if (CurrentContest.Size <= 500)
                {
                    CurrentContestStatus.FinalEntryIdScrape = true;
                }
            }
            else
            {
                CurrentContestStatus.FinalEntryIdScrape = true;
            }
            db.SaveChanges();
        }
        [Queue("ownership")]
        public void GetOwnership(int ContestGroupId)
        {
            List<double> IdList = new List<double>();

            var ContestList = (from e in db.ScrapeStatuses
                               where e.ContestGroupId == ContestGroupId && e.FinalEntryIdScrape == true
                               select e.ContestId).ToList();

            var DkClient = new RestClient("https://www.draftkings.com");
            IRestResponse response = Login(DkClient);

            foreach (int ContestId in ContestList)
            {

                ContestScrapeStatus CurrentContestStatus =
                    db.ScrapeStatuses
                    .Where(i => i.ContestId == ContestId)
                    .SingleOrDefault();

                var ContestEntryList = (from e in db.ContestEntries
                                        where e.ContestId == ContestId
                                        select e.EntryId).ToList();

                var LoginRequestTwo = new RestRequest("contest/gamecenter/" + ContestId, Method.GET);

                foreach (var cookie in response.Cookies)
                {
                    LoginRequestTwo.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
                }
                for (int i = 0; i < ContestEntryList.Count; i = i + 10)
                {
                    IdList.Clear();

                    for (int j = i; j < ContestEntryList.Count && j < i + 10; j++)
                    {
                        IdList.Add(ContestEntryList[j]);
                    }

                    FetchOwnership(IdList, ContestId, ContestGroupId, LoginRequestTwo, DkClient);
                }
                CurrentContestStatus.IsOwnershipCollected = true;
                db.SaveChanges();
            }
        }
        [Queue("ownership")]
        public void GetContestOwnership(int ContestId)
        {
            List<double> IdList = new List<double>();

            var DkClient = new RestClient("https://www.draftkings.com");
            IRestResponse response = Login(DkClient);
            
            ContestScrapeStatus CurrentContestStatus =
                db.ScrapeStatuses
                .Where(i => i.ContestId == ContestId)
                .SingleOrDefault();
            Contest CurrentContest =
                db.Contests
                .Where(i => i.ContestId == ContestId)
                .SingleOrDefault();
            var ContestEntryList = (from e in db.ContestEntries
                                    where e.ContestId == ContestId
                                    select e.EntryId).ToList();

            var LoginRequestTwo = new RestRequest("contest/gamecenter/" + ContestId, Method.GET);

            foreach (var cookie in response.Cookies)
            {
                LoginRequestTwo.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
            }
            for (int i = 0; i < ContestEntryList.Count; i = i + 10)
            {
                IdList.Clear();

                for (int j = i; j < ContestEntryList.Count && j < i + 10; j++)
                {
                    IdList.Add(ContestEntryList[j]);
                }

                FetchOwnership(IdList, ContestId, CurrentContest.ContestGroupId, LoginRequestTwo, DkClient);
            }
            CurrentContestStatus.IsOwnershipCollected = true;
            db.SaveChanges();
        }
        public void StoreOwnership(UserPlayerDataRootJson DkResponse, int ContestId)
        {
            bool ContestPlayerOwnershipExists;

            foreach (KeyValuePair<string, List<UserPlayerDataJson>> EntryData in DkResponse.data)
            {
                foreach (UserPlayerDataJson UserPlayer in EntryData.Value)
                {
                    ContestPlayer CurrentContestPlayer = db.ContestPlayers
                       .Where(ccp => ccp.ContestId == ContestId && ccp.PlayerId == UserPlayer.pid)
                       .SingleOrDefault();
                    ContestPlayerOwnershipExists = db.ContestPlayers.Any(ccp => ccp.ContestId == ContestId && ccp.PlayerId == UserPlayer.pid);
                    if (ContestPlayerOwnershipExists)
                    {
                        if (!float.IsNaN(CurrentContestPlayer.Ownership))
                        {
                            CurrentContestPlayer.Ownership = UserPlayer.pd;
                            db.SaveChanges();
                        }
                        else
                        {
                            Debug.WriteLine("NaN");
                        }
                    }
                    else
                    {
                        ContestPlayer newPlayer = new ContestPlayer();
                        newPlayer.ContestId = ContestId;
                        newPlayer.PlayerId = UserPlayer.pid;
                        newPlayer.Ownership = UserPlayer.pd;
                        newPlayer.ContestId = ContestId;
                        db.ContestPlayers.Add(newPlayer);
                        db.SaveChanges();
                    }
                }
            }
        }
        public void FetchOwnership(List<double> IdList, int ContestId, int ContestGroupId, RestRequest LoginRequestTwo, RestClient DkClient)
        {
            var EntryRequestObject = new RequestObject
            {
                idList = IdList,
                reqTs = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds,
                contestId = ContestId,
                draftGroupId = ContestGroupId
            };

            LoginRequestTwo.AddHeader("Content-type", "application/json");
            LoginRequestTwo.AddJsonBody(EntryRequestObject); // AddJsonBody serializes the object automatically
            Thread.Sleep(2000);
            IRestResponse entryResponse = DkClient.Execute(LoginRequestTwo);
            UserPlayerDataRootJson DkResponse = JsonConvert.DeserializeObject<UserPlayerDataRootJson>(entryResponse.Content);

            StoreOwnership(DkResponse, ContestId);
        }
        public IRestResponse Login(RestClient DkClient)
        {
            Random Rand = new Random();
            int RandomInt = Rand.Next(LoginList.Count);
            string LoginInfo = LoginList[RandomInt];

            var loginRequest = new RestRequest("account/login", Method.POST);
            loginRequest.AddParameter("layoutType", "3");
            loginRequest.AddParameter("login", LoginInfo);
            loginRequest.AddParameter("password", LoginInfo);
            loginRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            IRestResponse response = DkClient.Execute(loginRequest);
            return response;
        }
    }
}
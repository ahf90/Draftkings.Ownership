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
        private static readonly List<string> LoginList = new List<string> { "p4cnh7e1py", "lr4tgivbxh", "hw54585nzh", "k0oxzlg4et", "neimd335qv", "num1kingsfan" };

        public string GcSource(string ContestId)
        {
            Random Rand = new Random();
            int RandomInt = Rand.Next(LoginList.Count);
            string LoginInfo = LoginList[RandomInt];
            var client = new RestClient("https://www.draftkings.com");
            
            var loginRequest = new RestRequest("account/login", Method.POST);
            loginRequest.AddParameter("layoutType", "3");
            loginRequest.AddParameter("login", LoginInfo);
            loginRequest.AddParameter("password", LoginInfo);
            // layoutType = 3 & login = whatmeansdfs23 & password = whatmeansdfs23 & returnUrl = &recaptchaResponse = &profilingSessionId = null
            loginRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            IRestResponse response = client.Execute(loginRequest);

            var loginRequestTwo = new RestRequest("contest/gamecenter/" + ContestId, Method.GET);
            foreach (var cookie in response.Cookies)
            {
                loginRequestTwo.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
            }
            Thread.Sleep(5000);
            IRestResponse entryResponse = client.Execute(loginRequestTwo);
            //Regex LoginFail = Regex.IsMatch(entryResponse.ResponseUri, "sitelogin");
            //if (LoginFail)
            //{
            //    RandomInt = Rand.Next(LoginList.Count);
            //    LoginInfo = LoginList[RandomInt];
            //}
            return entryResponse.Content;

        }
        [Queue("entryids")]
        public void ContestGroupFetchEntryIds(int ContestGroupId)
        {
            bool ContestEntryExists;

            var ContestList = (from e in db.ScrapeStatuses
                               where e.ContestGroupId == ContestGroupId && e.FinalEntryIdScrape == false
                               select e.ContestId).ToList();

            Debug.WriteLine(ContestGroupId.ToString());

            Random Rand = new Random();
            int RandomInt = Rand.Next(LoginList.Count);
            string LoginInfo = LoginList[RandomInt];

            var client = new RestClient("https://www.draftkings.com");
            var loginRequest = new RestRequest("account/login", Method.POST);
            loginRequest.AddParameter("layoutType", "3");
            loginRequest.AddParameter("login", LoginInfo);
            loginRequest.AddParameter("password", LoginInfo);
            loginRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            IRestResponse response = client.Execute(loginRequest);

            foreach (int ContestId in ContestList)
            {
                ContestScrapeStatus CurrentContestStatus = 
                    db.ScrapeStatuses
                    .Where(i => i.ContestId == ContestId)
                    .SingleOrDefault();

                Contest CurrentContest = 
                    db.Contests
                    .Where(i => i.ContestId == ContestId)
                    .SingleOrDefault();

                Thread.Sleep(6000);
                var loginRequestTwo = new RestRequest("contest/gamecenter/" + ContestId, Method.GET);
                foreach (var cookie in response.Cookies)
                {
                    loginRequestTwo.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
                }
                
                IRestResponse entryResponse = client.Execute(loginRequestTwo);

                string Source = entryResponse.Content;
                int IdStartIndex = Regex.Match(Source, "var teams ").Index;
                IdStartIndex = IdStartIndex + 12;

                Source = Source.Substring(IdStartIndex, Source.Length - IdStartIndex);
                int SemiColonIndex = Regex.Match(Source, ";").Index;
                Source = Source.Substring(0, SemiColonIndex);
                List<ContestEntryJson> IdList = JsonConvert.DeserializeObject<List<ContestEntryJson>>(Source);
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
        }
        [Queue("entryids")]
        public void FetchEntryIds(int ContestId)
        {
            bool ContestEntryExists;
            Debug.WriteLine(ContestId.ToString());
            string Source = GcSource(ContestId.ToString());
            int IdStartIndex = Regex.Match(Source, "var teams ").Index;
            IdStartIndex = IdStartIndex + 12;

            Source = Source.Substring(IdStartIndex, Source.Length - IdStartIndex);
            int SemiColonIndex = Regex.Match(Source, ";").Index;
            Source = Source.Substring(0, SemiColonIndex);
            List<ContestEntryJson> IdList = JsonConvert.DeserializeObject<List<ContestEntryJson>>(Source);
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

            // db.SaveChanges();
            BackgroundJob.Enqueue(() => CreateControllerInstance.ContestPlayers(ContestId));
            //Wait 10 seconds to send next job
            //To avoid DK detection/IP banning
            Thread.Sleep(5000);

        }
        [Queue("ownership")]
        public void GetOwnership(int ContestGroupId)
        {

            bool ContestPlayerOwnershipExists;
            List<double> IdList = new List<double>();
            Random Rand = new Random();
            int RandomInt = Rand.Next(LoginList.Count);
            string LoginInfo = LoginList[RandomInt];

            var ContestList = (from e in db.ScrapeStatuses
                               where e.ContestGroupId == ContestGroupId && e.FinalEntryIdScrape == true
                               select e.ContestId).ToList();

            var client = new RestClient("https://www.draftkings.com");

            var loginRequest = new RestRequest("account/login", Method.POST);
            loginRequest.AddParameter("layoutType", "3");
            loginRequest.AddParameter("login", LoginInfo);
            loginRequest.AddParameter("password", LoginInfo);
            loginRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            IRestResponse response = client.Execute(loginRequest);

            foreach (int ContestId in ContestList)
            {

                ContestScrapeStatus CurrentContestStatus =
                    db.ScrapeStatuses
                    .Where(i => i.ContestId == ContestId)
                    .SingleOrDefault();

                var ContestEntryList = (from e in db.ContestEntries
                                        where e.ContestId == ContestId
                                        select e.EntryId).ToList();


                loginRequest.Parameters.Clear();
                loginRequest.Resource = "contest/getusercontestplayers";

                foreach (var cookie in response.Cookies)
                {
                    loginRequest.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
                }
                for (int i = 0; i < ContestEntryList.Count; i = i + 10)
                {
                    IdList.Clear();


                    for (int j = i; j < ContestEntryList.Count && j < i + 10; j++)
                    {
                        IdList.Add(ContestEntryList[j]);
                    }

                    var EntryRequestObject = new RequestObject
                    {
                        idList = IdList,
                        reqTs = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds,
                        contestId = ContestId,
                        draftGroupId = ContestGroupId

                    };
                    var json = loginRequest.JsonSerializer.Serialize(EntryRequestObject);

                    loginRequest.AddHeader("Content-type", "application/json");
                    loginRequest.AddJsonBody(EntryRequestObject); // AddJsonBody serializes the object automatically
                    Thread.Sleep(2000);
                    IRestResponse entryResponse = client.Execute(loginRequest);
                    UserPlayerDataRootJson dkr = JsonConvert.DeserializeObject<UserPlayerDataRootJson>(entryResponse.Content);

                    // foreach (UserPlayerData pl in dkr.data.UserPlayerData)s
                    foreach (KeyValuePair<string, List<UserPlayerDataJson>> EntryData in dkr.data)
                    {
                        foreach (UserPlayerDataJson UserPlayer in EntryData.Value)
                        {
                            ContestPlayer CurrentContestPlayer = db.ContestPlayers
                               .Where(ccp => ccp.ContestId == ContestId && ccp.PlayerId == UserPlayer.pid)
                               .SingleOrDefault();
                            ContestPlayerOwnershipExists = db.ContestPlayers.Any(ccp => ccp.ContestId == ContestId && ccp.PlayerId == UserPlayer.pid);
                            if (ContestPlayerOwnershipExists)
                            {
                                //if (!float.IsNaN(CurrentContestPlayer.Ownership))
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
                CurrentContestStatus.IsOwnershipCollected = true;
                db.SaveChanges();
            }
        }
        
    }
}
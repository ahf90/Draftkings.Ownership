﻿using System.Diagnostics;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Net;
using System.Threading;
using Draftkings.Ownership.Models;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using RestSharp;
using Hangfire;
using System.Net.Mail;
using HtmlAgilityPack;

namespace Draftkings.Ownership.Controllers
{
    public class LobbyController : Controller
    {
        private FantasyContestsDBContextDk Database = new FantasyContestsDBContextDk();
        private ScrapeController ContestScrapeControllerInstance = new ScrapeController();
        private static readonly Dictionary<string, int> SportIdDict = new Dictionary<string, int> { { "NFL", 1 }, { "NBA", 2 }, { "MLB", 3 }, { "NHL", 4 }, { "PGA", 5 }, { "LOL", 6 }, { "SOC", 7 }, { "NAS", 8 }, { "MMA", 9 }, { "GOLF", 5 }, { "CFL", 10 } };

        public void Index()
        {
            //Normal check to make sure hangfire tasks are not on pause
            string AccessDeniedFlag = System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"];
            if (AccessDeniedFlag == "true")
            {
                BackgroundJob.Schedule(
                         () => Index(),
                         TimeSpan.FromHours(1));
                return;
            }

            var LobbyClient = new RestClient("https://www.draftkings.com");
            var LobbyRequest = new RestRequest("lobby/getcontests", Method.GET);
            LobbyRequest.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36 Edge/13.10586");
            IRestResponse LobbyResponse = LobbyClient.Execute(LobbyRequest);

            //If I get a 403, then exit
            if (!CheckAccess(LobbyResponse.StatusCode)) { return; }

            string LobbyOutput = LobbyResponse.Content;
            LobbyRoot LobbyObject = JsonConvert.DeserializeObject<LobbyRoot>(LobbyOutput);
            foreach (ContestGroupJson ContestGroupElement in LobbyObject.DraftGroups)
            {
                bool ContestGroupExists;
                ContestGroupExists = Database.ContestGroups.Any(dg => dg.ContestGroupId == ContestGroupElement.DraftGroupId);
                if (!ContestGroupExists)
                {
                    //Sleep for 5 seconds to avoid 403
                    Thread.Sleep(5000);
                    //The ContestTypeId integer seems to not matter
                    var SalaryRequest = new RestRequest("lineup/getavailableplayers?contestTypeId=28&draftGroupId=" + ContestGroupElement.DraftGroupId.ToString(), Method.GET);
                    IRestResponse SalaryResponse = LobbyClient.Execute(SalaryRequest);

                    if (!CheckAccess(SalaryResponse.StatusCode)) { return; }

                    string SalaryOutput = SalaryResponse.Content;
                    SalaryRoot Salaries = JsonConvert.DeserializeObject<SalaryRoot>(SalaryOutput);

                    //Sometimes DK posts contests and contestgroups with blank salaries.  This happens when the contest start is too far away, and salaries have not been made yet.
                    //If this is the case, skip it. 
                    if (Salaries.playerList.Count == 0 || Salaries.teamList.Count == 0) { continue; }

                    foreach (PlayerJson PlayerElement in Salaries.playerList)
                    {
                        DraftGroupPlayer NewDraftGroupPlayer = new DraftGroupPlayer();
                        NewDraftGroupPlayer.PlayerId = PlayerElement.pid;
                        NewDraftGroupPlayer.Salary = PlayerElement.s;
                        NewDraftGroupPlayer.DraftGroupId = ContestGroupElement.DraftGroupId;
                        Database.DraftGroupPlayers.Add(NewDraftGroupPlayer);
                    }
                    ContestGroup NewContestGroup = new ContestGroup();
                    NewContestGroup.ContestGroupId = ContestGroupElement.DraftGroupId;
                    NewContestGroup.DraftGroupId = ContestGroupElement.DraftGroupId;
                    NewContestGroup.SourceId = 4;
                    //Use the SportIdDict to convert DraftKings's string to FantasyLab's integer-based sport label system
                    NewContestGroup.SportId = SportIdDict[ContestGroupElement.Sport];
                    NewContestGroup.ContestGroupName = ContestGroupElement.ContestStartTimeSuffix;
                    NewContestGroup.GameCount = ContestGroupElement.GameCount;
                    NewContestGroup.LastGameStart = CalculateLastGameStart(Salaries);
                    NewContestGroup.ContestStartDate = CalculateFirstGameStart(Salaries);

                    Database.ContestGroups.Add(NewContestGroup);
                    Database.SaveChanges();

                    //All times are stored in UTC
                    DateTime CurrentTime = DateTime.UtcNow;

                    TimeSpan SpanUntilStart = NewContestGroup.ContestStartDate.Subtract(CurrentTime);
                    TimeSpan SpanUntilEnd = NewContestGroup.LastGameStart.Subtract(CurrentTime);

                    double SpanUntilStartDouble = SpanUntilStart.TotalMinutes;
                    double SpanUntilEndDouble = SpanUntilEnd.TotalMinutes;

                    //Scrape for ownership 250 minutes after the last game starts.
                    double SpanUntilOwnershipScrapeDouble = SpanUntilEnd.TotalMinutes + 250.0;

                    //Giving DK a 3 minute cushion before we send request
                    //I was hitting a few "contest not started" errors if I sent immediately.
                    SpanUntilStartDouble += 3;
                    SpanUntilEndDouble += 3;

                    BackgroundJob.Enqueue(
                        () => SelectContests(ContestGroupElement.DraftGroupId, ContestGroupElement.Sport, true));
                    BackgroundJob.Schedule(
                        () => ContestScrapeControllerInstance.ContestGroupFetchEntryIds(ContestGroupElement.DraftGroupId),
                        TimeSpan.FromMinutes(SpanUntilStartDouble));
                    BackgroundJob.Schedule(
                        () => ContestScrapeControllerInstance.ContestGroupFetchEntryIds(ContestGroupElement.DraftGroupId),
                        TimeSpan.FromMinutes(SpanUntilEndDouble));
                    BackgroundJob.Schedule(
                        () => ContestScrapeControllerInstance.GetOwnership(ContestGroupElement.DraftGroupId),
                        TimeSpan.FromMinutes(SpanUntilOwnershipScrapeDouble));
                }
                else
                {
                    BackgroundJob.Enqueue(
                       () => SelectContests(ContestGroupElement.DraftGroupId, ContestGroupElement.Sport, false));
                }
            }
        }
        [Queue("contestload")]
        public void SelectContests(int DraftGroupId, string SportAbbr, bool FirstRun)
        {
            //these arrays will hold the ContestIds of the contests we end up scraping.
            int[] LargestTournament = new int[2] { 0, 0 };
            int[] LargestMultiplier = new int[2] { 0, 0 };
            int[] SelectedTournaments = new int[4] { 0, 0, 0, 0 };

            bool NotTournament = false;

            Thread.Sleep(3000);

            var LobbyClient = new RestClient("https://www.draftkings.com");
            var LobbyRequest = new RestRequest("lobby/getcontests?sport=" + SportAbbr, Method.GET);
            LobbyRequest.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36 Edge/13.10586");
            IRestResponse LobbyResponse = LobbyClient.Execute(LobbyRequest);

            if (!CheckAccess(LobbyResponse.StatusCode)) { return; }

            string LobbyOutput = LobbyResponse.Content;
            LobbyRoot LobbyObject = JsonConvert.DeserializeObject<LobbyRoot>(LobbyOutput);
            				
            foreach (ContestJson ContestElement in LobbyObject.Contests)
            {
                if(FirstRun)
                {
                    if (ContestElement.dg == DraftGroupId)
                    {
                        NotTournament = false;
                        //The "IsDoubleUp" attribute is never set to false.  If the contest is not a double up, then the attribute does not exist. If it exists, then it's true. Same idea with 50/50s.
                        //This is why we cannot do:
                        //if (ContestElement.attr.IsDoubleUp == "true" || ContestElement.attr.IsFiftyfifty == "true")
                        //Instead, we check to see whether the attribute exists
                        if (ContestElement.attr.GetType().GetProperty("IsDoubleUp") != null)
                        {
                            //then, just to be sure, make sure it is set to true
                            if (ContestElement.attr.IsDoubleUp == "true")
                            {
                                NotTournament = true;
                                if (ContestElement.m > LargestMultiplier[0] && ContestElement.a > 1)
                                {
                                    if (ContestElement.m > LargestMultiplier[1])
                                    {
                                        LargestMultiplier[1] = ContestElement.m;
                                        SelectedTournaments[0] = ContestElement.id;
                                    }
                                    else
                                    {
                                        LargestMultiplier[0] = ContestElement.m;
                                        SelectedTournaments[1] = ContestElement.id;
                                    }
                                }
                            }
                        }
                        else if (ContestElement.attr.GetType().GetProperty("IsFiftyfifty") != null)
                        {
                            if (ContestElement.attr.IsFiftyfifty == "true")
                            {
                                NotTournament = true;
                                if (ContestElement.m > LargestMultiplier[0] && ContestElement.a > 1)
                                {
                                    if (ContestElement.m > LargestMultiplier[1])
                                    {
                                        LargestMultiplier[1] = ContestElement.m;
                                        SelectedTournaments[0] = ContestElement.id;
                                    }
                                    else
                                    {
                                        LargestMultiplier[0] = ContestElement.m;
                                        SelectedTournaments[1] = ContestElement.id;
                                    }
                                }
                            }
                        }
                        if (NotTournament == false)
                        {
                            if (ContestElement.m > LargestTournament[0] && ContestElement.a > 1)
                            {
                                if (ContestElement.m > LargestTournament[1])
                                {
                                    LargestTournament[1] = ContestElement.m;
                                    SelectedTournaments[2] = ContestElement.id;
                                }
                                else
                                {
                                    LargestTournament[0] = ContestElement.m;
                                    SelectedTournaments[3] = ContestElement.id;
                                }
                            }
                        }
                    }

					bool ContestExists;
                    ContestExists = Database.Contests.Any(cont => cont.ContestId == ContestElement.id);
                    if (ContestExists == false && ContestElement.attr.IsGuaranteed == "true")
                    {
                        string ContestType = "";
                        if (ContestElement.attr.GetType().GetProperty("IsQualifier") != null)
                        {
                            //If so, check to make sure the atrribute is set to true
                            if (ContestElement.attr.IsQualifier == "true")
                            {
                                if (ContestElement.n.IndexOf("Satellite") != -1 || ContestElement.n.IndexOf("SUPERSat") != -1)
                                {
                                    ContestType = "Satellite";
                                }
                                else if (ContestElement.n.IndexOf("Qualifier") != -1)
                                {
                                    ContestType = "Qualifier";
                                }
                                else
                                {
                                    ContestType = "Satellite";
		                        }
                            }
                        }
                        else if (ContestElement.attr.GetType().GetProperty("IsDoubleUp") != null && ContestElement.attr.GetType().GetProperty("IsFiftyfifty") != null)
                        {
                            if (ContestElement.attr.IsDoubleUp == "true" && ContestElement.attr.IsFiftyfifty == "true")
                            {
                                if (ContestElement.n.IndexOf("Double Up") != -1)
                                {
                                    ContestType = "Double Up";
                                }
                                else if (ContestElement.n.IndexOf("50/50") != -1)
                                {
                                    ContestType = "Fifty-Fifty";
                                }
                                else
                                {
                                    ContestType = "Unknown";
                                }
                            }
                            else if (ContestElement.attr.IsDoubleUp == "true")
                            {
                                ContestType = "Double Up";
                            }
                            else if (ContestElement.attr.IsFiftyfifty == "true")
                            {
                                ContestType = "Fifty-Fifty";
                            }
                            else
                            {
                                ContestType = "Unknown";
                            }
                        }
                        else if (ContestElement.attr.GetType().GetProperty("IsFiftyfifty") != null)
                        {
                            if (ContestElement.attr.IsFiftyfifty == "true")
                            {
                                ContestType = "Fifty-Fifty";
                            }
                            else
                            {
                                ContestType = "Unknown";
                            }
                        }
                        else if (ContestElement.attr.GetType().GetProperty("IsDoubleUp") != null)
                        {
                            if (ContestElement.attr.IsDoubleUp == "true")
                            {
                                ContestType = "Double Up";
                            }
                            else
                            {
                                ContestType = "Unknown";
                            }
                        }
                        else if (ContestElement.attr.GetType().GetProperty("Multiplier") != null)
                        {
                            if (ContestElement.attr.Multiplier == "true")
                            {
                                ContestType = "Multiplier";
                            }
                            else
                            {
                                ContestType = "Unknown";
                            }
                        }
                        else if (ContestElement.attr.GetType().GetProperty("League") != null)
                        {
                            if (ContestElement.attr.League == "true")
                            {
                                ContestType = "League";
                            }
                            else
                            {
                                ContestType = "Unknown";
                            }
                        }
                        else
                        {
                            ContestType = "Tournament";
                        }
                        Contest NewContest = new Contest();
                        NewContest.ContestId = ContestElement.id;
                        NewContest.DraftGroupId = ContestElement.dg; // DraftGroupId
                        NewContest.ContestGroupId = ContestElement.dg;
                        NewContest.ContestTitle = ContestElement.n;
                        NewContest.Size = ContestElement.m;
                        NewContest.MultiEntry = ContestElement.mec;
                        NewContest.EntryFee = ContestElement.a;
                        NewContest.ContestType = ContestType;
                        Database.Contests.Add(NewContest);
                        Database.SaveChanges();

                        Prizes NewPrize = new Prizes();
                        NewPrize.ContestId = ContestElement.id;
                        NewPrize.TotalPrizes = ContestElement.po; // DraftGroupId
                        //NewPrize.Summary = ContestElement.dg;
                       // NewPrize.WinnerCount = ContestElement.n;
                        Database.Prize.Add(NewPrize);
                        Database.SaveChanges();
                    }
                }
            }

            if (FirstRun)
            {
                // Check to make sure we've selected 4 contests
                // This check is necessary because DK will often put out one big contest way ahead of time, but no others
                // We must wait until all contests are out to declare this a "FirstRun"
                foreach (int SelectedContest in SelectedTournaments)
                {
                    if (SelectedContest == 0)
                    {
                        FirstRun = false;
                        Thread.Sleep(10000);
                        return;
                    }
                }

                foreach (ContestJson ContestElement in LobbyObject.Contests)
                {

                    if (Array.IndexOf(SelectedTournaments, ContestElement.id) != -1)
                    {
                        bool StatusExists = Database.ScrapeStatuses.Any(pl => pl.ContestId == ContestElement.id);
                        if (!StatusExists)
                        {
                            GetContestPrize(ContestElement.id);

                            ContestScrapeStatus NewContestStatus = new ContestScrapeStatus();
                            NewContestStatus.ContestId = ContestElement.id;
                            NewContestStatus.ContestGroupId = ContestElement.dg;
                            NewContestStatus.InitialEntryIdScrape = false;
                            NewContestStatus.FinalEntryIdScrape = false;
                            Database.ScrapeStatuses.Add(NewContestStatus);
                            Database.SaveChanges();
                            BackgroundJob.Enqueue(() => CreateContestPlayers(ContestElement.id));
                        }

                    }
                }
            }

            Thread.Sleep(10000);
        }
        public void GetContestPrize(int ContestId)
        {
            try
            {
                
            
                int FinalPaymentPlace;
                string ContestIdString = ContestId.ToString();

                // Query the database for the row to be updated.
                Prizes ContestPrize =
                    (from prize in Database.Prize
                    where prize.ContestId == ContestId
                    select prize).SingleOrDefault();

                HtmlWeb WebHtml = new HtmlWeb();
                HtmlDocument DocumentHtml = WebHtml.Load("https://www.draftkings.com/contest/detailspop?contestId=" + ContestIdString);
                HtmlNode[] TrNodes = DocumentHtml.DocumentNode.SelectNodes("//div[@class='dk-black-rounded-panel prize-payouts']//tr").ToArray();
                HtmlNode FinalTrNode = TrNodes[TrNodes.Length - 1];
               //HtmlNode FinalTdNode = FinalTrNode.LastChild.S.Last();
               //string FinalPaymentString = FinalTdNode.InnerHtml;
                string FinalPaymentString = FinalTrNode.ChildNodes[1].InnerHtml;
                Regex CharRegex = new Regex("[^0-9 -]");
                FinalPaymentString = CharRegex.Replace(FinalPaymentString, "");
                string[] FinalPaymentSplit = FinalPaymentString.Split('-');
                if (FinalPaymentSplit.Length > 1)
                {
                    FinalPaymentPlace = Int32.Parse(FinalPaymentSplit[1]);
                }
                else
                {
                    FinalPaymentPlace = Int32.Parse(FinalPaymentSplit[0]);
                }

                ContestPrize.WinnerCount = FinalPaymentPlace;

                try
                {
                    Database.SaveChanges();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        [Queue("playercreate")]
        public void CreateContestPlayers(int ContestId)
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
        public DateTime CalculateLastGameStart(SalaryRoot Salaries)
        {
            DateTime LastGame = new DateTime();
            LastGame = DateTime.MinValue;
            foreach (KeyValuePair<string, Fixture> FixtureElement in Salaries.teamList)
            {
                string numbersOnly = Regex.Replace(FixtureElement.Value.tz, "[^0-9]", "");
                FixtureElement.Value.tzEpoch = Convert.ToDouble(numbersOnly);
                FixtureElement.Value.GameStart = new DateTime(1970, 1, 1).AddMilliseconds(FixtureElement.Value.tzEpoch);

                int TimeComparison = DateTime.Compare(LastGame, FixtureElement.Value.GameStart);

                if (LastGame == DateTime.MinValue)
                //LastGame has not been assigned (this is first fixture in loop)
                {
                    LastGame = FixtureElement.Value.GameStart;
                }
                else if (TimeComparison < 0)
                //LastGame is earlier than FixtureElement.GameStart;
                {
                    LastGame = FixtureElement.Value.GameStart;
                }
            }
            return LastGame;

        }
        public DateTime CalculateFirstGameStart(SalaryRoot Salaries)
        {
            DateTime FirstGame = new DateTime();
            FirstGame = DateTime.MinValue;

            foreach (KeyValuePair<string, Fixture> FixtureElement in Salaries.teamList)
            {
                string numbersOnly = Regex.Replace(FixtureElement.Value.tz, "[^0-9]", "");
                FixtureElement.Value.tzEpoch = Convert.ToDouble(numbersOnly);
                FixtureElement.Value.GameStart = new DateTime(1970, 1, 1).AddMilliseconds(FixtureElement.Value.tzEpoch);

                int TimeComparison = DateTime.Compare(FirstGame, FixtureElement.Value.GameStart);

                if (FirstGame == DateTime.MinValue)
                //First has not been assigned (this is first fixture in loop)
                {
                    FirstGame = FixtureElement.Value.GameStart;
                }
                else if (TimeComparison > 0)
                //First is later than FixtureElement.GameStart;
                {
                    FirstGame = FixtureElement.Value.GameStart;
                }
            }

            return FirstGame;

        }
        public bool CheckAccess(HttpStatusCode StatusCode)
        {
            int NumericStatusCode = (int)StatusCode;
            if (NumericStatusCode == 403)
            {
                //If I get a 403
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("dko.errors.fl@gmail.com");
                mail.To.Add("ahfriedman09@gmail.com");
                mail.Subject = "403 Error Access Denied";
                mail.Body = "403 Error Access Denied at " + DateTime.Now.ToString();

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("dko.errors.fl", "4ownershipWoes");
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                DisposeHangfire();
                return false;
            }
            else
            {
                return true;
            }
        }
        public void StartLobbyCron()
        {
            RecurringJob.AddOrUpdate(
                () => Index(),
                "0 5,11,16,20 * * *");
            //Send CRON job to scrape lobby for contests at 5am, 11am, 4pm, and 8pm
        }
        public void DisposeHangfire()
        {
            System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"] = "true";
        }
        
    }
}
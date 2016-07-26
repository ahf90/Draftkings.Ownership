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
using System.Net;
using System.Net.Mail;
using System.Collections;

namespace Draftkings.Ownership.Controllers
{
    public class ScrapeController : Controller
    {
        private FantasyContestsDBContextDk db = new FantasyContestsDBContextDk();
        private CreateController CreateControllerInstance = new CreateController();

        [Queue("entryids")]
        public void ContestGroupFetchEntryIds(int ContestGroupId)
        {
            string AccessDeniedFlag = System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"];
            if (AccessDeniedFlag == "true")
            {
                BackgroundJob.Schedule(
                        () => ContestGroupFetchEntryIds(ContestGroupId),
                        TimeSpan.FromDays(1));
                return;
            }
            string CaptchaFlag = System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"];
            if (CaptchaFlag == "true")
            {
                BackgroundJob.Schedule(
                        () => ContestGroupFetchEntryIds(ContestGroupId),
                        TimeSpan.FromHours(2));
                return;
            }

            var ContestList = (from e in db.ScrapeStatuses
                               where e.ContestGroupId == ContestGroupId && e.FinalEntryIdScrape == false
                               select e.ContestId).ToList();

            var DkClient = new RestClient("https://www.draftkings.com");
            IRestResponse LoginResponse = Login(DkClient);

            if (LoginResponse.ErrorMessage == "Captcha" || LoginResponse.ErrorMessage == "403")
            {
                BackgroundJob.Schedule(
                        () => ContestGroupFetchEntryIds(ContestGroupId),
                        TimeSpan.FromHours(6));
                return;
            }

            RestRequest LoginRequestTwo = new RestRequest("contest/gamecenter/", Method.GET);
            foreach (var cookie in LoginResponse.Cookies)
            {
                LoginRequestTwo.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
            }

            foreach (int ContestId in ContestList)
            {
                FetchContestEntryIds(LoginRequestTwo, DkClient, ContestId, false);
            }
        }
        [Queue("entryids")]
        public void FetchSingular(int ContestId)
        {
            var DkClient = new RestClient("https://www.draftkings.com");
            IRestResponse LoginResponse = Login(DkClient);

            if (LoginResponse.ErrorMessage == "Captcha" || LoginResponse.ErrorMessage == "403")
            {
                BackgroundJob.Schedule(
                        () => FetchSingular(ContestId),
                        TimeSpan.FromHours(6));
                return;
            }


            RestRequest LoginRequestTwo = new RestRequest("contest/gamecenter/", Method.GET);
            foreach (var cookie in LoginResponse.Cookies)
            {
                LoginRequestTwo.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
            }

            FetchContestEntryIds(LoginRequestTwo, DkClient, ContestId, true);

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
        public void FetchContestEntryIds(RestRequest LoginRequestTwo, RestClient DkClient, int ContestId, bool manual)
        {
            string AccessDeniedFlag = System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"];
            if (AccessDeniedFlag == "true")
            {
                BackgroundJob.Schedule(
                        () => FetchSingular(ContestId),
                        TimeSpan.FromDays(1));
                return;
            }
            string CaptchaFlag = System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"];
            if (CaptchaFlag == "true")
            {
                BackgroundJob.Schedule(
                        () => FetchSingular(ContestId),
                        TimeSpan.FromHours(2));
                return;
            }

            Contest CurrentContest = db.Contests
                                    .Where(i => i.ContestId == ContestId)
                                    .SingleOrDefault();

            if (CurrentContest == null) { return; }

            Thread.Sleep(6000);

            LoginRequestTwo.Resource = "contest/gamecenter/" + ContestId;

            IRestResponse ContestResponse = DkClient.Execute(LoginRequestTwo);

            HttpStatusCode StatusCode = ContestResponse.StatusCode;
            int NumericStatusCode = (int)StatusCode;

            if (NumericStatusCode == 403)
            {
                //If we get a 403, then we need to reproxy.  
                //Sends an email and reschedules task for later.
                SendMail("403 Error", "403 Error encountered while trying to scrape gamecenter for " + ContestId.ToString() + ". Task rescheduled to 1 day from now.");

                BackgroundJob.Schedule(
                        () => FetchSingular(ContestId),
                        TimeSpan.FromDays(1));
                DisposeHangfire(false);
                return;
            }

            if (ContestResponse.ResponseUri.AbsolutePath == "/lobby")
            {
                //If we are sent to the lobby, then the contest did not run.
                SendMail("Contest: " + ContestId.ToString() + ", ContestGroup: " + CurrentContest.ContestGroupId.ToString() + " lobby redirect failure",
                    "Task failed. Attempt to scrape gamecenter redirected to lobby.");
                return;
            }

            int ErrorCount = 0;

            while (NumericStatusCode != 200 && ErrorCount <= 5)
            {
                //Sleep for 8 seconds and try again
                //Remember that do-while loops always execute once
                Thread.Sleep(8000);
                ErrorCount++;
                ContestResponse = DkClient.Execute(LoginRequestTwo);
                StatusCode = ContestResponse.StatusCode;
                NumericStatusCode = (int)StatusCode;

                if (ErrorCount > 5)
                {
                    SendMail("FetchContestEntryIds for " + ContestId.ToString() + " failed to get a 200", "Task rescheduled to 1 day from now");
                    BackgroundJob.Schedule(
                        () => FetchSingular(ContestId),
                        TimeSpan.FromDays(1));
                    return;
                }
            }

            string Source = ContestResponse.Content;
            int IdStartIndex = Regex.Match(Source, "var teams ").Index;
            IdStartIndex = IdStartIndex + 12;

            Source = Source.Substring(IdStartIndex, Source.Length - IdStartIndex);
            int SemiColonIndex = Regex.Match(Source, ";").Index;
            Source = Source.Substring(0, SemiColonIndex);
            List<ContestEntryJson> IdList = JsonConvert.DeserializeObject<List<ContestEntryJson>>(Source);

            StoreEntryIds(IdList, ContestId);
            if (manual == false)
            {
                ContestScrapeStatus CurrentContestStatus = db.ScrapeStatuses
                                                           .Where(i => i.ContestId == ContestId)
                                                           .SingleOrDefault();
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
        [Queue("ownership")]
        public void GetOwnership(int ContestGroupId)
        {
            string AccessDeniedFlag = System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"];
            if (AccessDeniedFlag == "true")
            {
                BackgroundJob.Schedule(
                        () => GetOwnership(ContestGroupId),
                        TimeSpan.FromDays(1));
                return;
            }
            string CaptchaFlag = System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"];
            if (CaptchaFlag == "true")
            {
                BackgroundJob.Schedule(
                        () => GetOwnership(ContestGroupId),
                        TimeSpan.FromHours(2));
                return;
            }
            List<double> IdList = new List<double>();

            var ContestList = (from e in db.ScrapeStatuses
                               where e.ContestGroupId == ContestGroupId && e.FinalEntryIdScrape == true
                               select e.ContestId).ToList();

            var DkClient = new RestClient("https://www.draftkings.com");
            IRestResponse LoginResponse = Login(DkClient);
            if (LoginResponse.ErrorMessage == "Captcha" || LoginResponse.ErrorMessage == "403")
            {
                BackgroundJob.Schedule(
                        () => GetOwnership(ContestGroupId),
                        TimeSpan.FromHours(6));
                return;
            }

            foreach (int ContestId in ContestList)
            {
                AccessDeniedFlag = System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"];
                if (AccessDeniedFlag == "true")
                {
                    BackgroundJob.Schedule(
                            () => GetContestOwnership(ContestGroupId),
                            TimeSpan.FromDays(1));
                    continue;
                }
                CaptchaFlag = System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"];
                if (CaptchaFlag == "true")
                {
                    BackgroundJob.Schedule(
                            () => GetContestOwnership(ContestGroupId),
                            TimeSpan.FromHours(2));
                    continue;
                }
                ContestScrapeStatus CurrentContestStatus =
                    db.ScrapeStatuses
                    .Where(i => i.ContestId == ContestId)
                    .SingleOrDefault();

                var ContestEntryList = (from e in db.ContestEntries
                                        where e.ContestId == ContestId
                                        select e.EntryId).ToList();

                var LoginRequestTwo = new RestRequest("contest/getusercontestplayers", Method.POST);

                foreach (var cookie in LoginResponse.Cookies)
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
            IRestResponse LoginResponse = Login(DkClient);

            if (LoginResponse.ErrorMessage == "Captcha" || LoginResponse.ErrorMessage == "403")
            {
                BackgroundJob.Schedule(
                        () => GetContestOwnership(ContestId),
                        TimeSpan.FromHours(6));
                return;
            }

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

            var LoginRequestTwo = new RestRequest("contest/getusercontestplayers", Method.POST);

            foreach (var cookie in LoginResponse.Cookies)
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
            if (CurrentContestStatus != null)
            {
                CurrentContestStatus.IsOwnershipCollected = true;
                db.SaveChanges();
            }
        }
        public void StoreOwnership(Dictionary<string, List<UserPlayerDataJson>> DkResponse, int ContestId)
        {
            bool ContestPlayerOwnershipExists;

            foreach (KeyValuePair<string, List<UserPlayerDataJson>> EntryData in DkResponse)
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
            Thread.Sleep(3000);
            IRestResponse OwnershipResponse = DkClient.Execute(LoginRequestTwo);
            HttpStatusCode StatusCode = OwnershipResponse.StatusCode;
            int NumericStatusCode = (int)StatusCode;

            if (NumericStatusCode == 403)
            {
                //If we get a 403, then we need to reproxy.  
                //Sends an email and reschedules task for later.
                SendMail("403 Error", "403 Error encountered while trying to scrape ownership for " + ContestId.ToString() + ". Task rescheduled to 1 day from now.");

                BackgroundJob.Schedule(
                        () => GetContestOwnership(ContestId),
                        TimeSpan.FromDays(1));
                DisposeHangfire(false);
                return;
            }

            int ErrorCount = 0;

            while (NumericStatusCode != 200 && ErrorCount <= 5)
            {
                //Sleep for 8 seconds and try again
                //Remember that do-while loops always execute once
                Thread.Sleep(8000);
                ErrorCount++;
                OwnershipResponse = DkClient.Execute(LoginRequestTwo);
                StatusCode = OwnershipResponse.StatusCode;
                NumericStatusCode = (int)StatusCode;

                if (ErrorCount > 5)
                {
                    SendMail("FetchOwnership for " + ContestId.ToString() + " failed to get a 200", "Task rescheduled to 1 day from now");
                    BackgroundJob.Schedule(
                        () => GetContestOwnership(ContestId),
                        TimeSpan.FromDays(1));
                    return;
                }
            }

            UserPlayerDataRootJson DkResponse = JsonConvert.DeserializeObject<UserPlayerDataRootJson>(OwnershipResponse.Content);

            StoreOwnership(DkResponse.data, ContestId);
        }
        public IRestResponse Login(RestClient DkClient)
        {
            List<String> LoginList = (from e in db.Logins
                                      select e.Username).ToList();

            Random Rand = new Random();
            string LoginInfo = LoginList[Rand.Next(LoginList.Count)];
            string RequestPath = "account/login";
            var loginRequest = new RestRequest(RequestPath, Method.POST);
            loginRequest.AddParameter("layoutType", "3");
            loginRequest.AddParameter("login", LoginInfo);
            loginRequest.AddParameter("password", LoginInfo);
            loginRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            IRestResponse LoginResponse = DkClient.Execute(loginRequest);

            HttpStatusCode StatusCode = LoginResponse.StatusCode;
            int NumericStatusCode = (int)StatusCode;

            if (NumericStatusCode == 403)
            {
                SendMail("403 Error", "Tasks put on hold.");
                DisposeHangfire(false);
                LoginResponse.ErrorMessage = "403";
                return LoginResponse;
            }

            DkLoginResponse LoginResponseObject = JsonConvert.DeserializeObject<DkLoginResponse>(LoginResponse.Content);

            if (LoginResponseObject.statusId == -1)
            {
                LoginInfo CurrentLogin = (from e in db.Logins
                                          where e.Username == LoginInfo
                                          select e).SingleOrDefault();
                db.Logins.Remove(CurrentLogin);
                db.SaveChanges();
                LoginResponse = Login(DkClient);
            }
            else if (LoginResponseObject.statusId == 0)
            {
                if (LoginResponseObject.message == "Incorrect captcha response.")
                {
                    SendMail("Captcha required", "Tasks put on hold for 2 hours.");
                    DisposeHangfire(true);
                    BackgroundJob.Schedule(
                        () => StartHangfire(true),
                        TimeSpan.FromHours(2));
                    LoginResponse.ErrorMessage = "Captcha";
                }
                else
                {
                    LoginInfo CurrentLogin = (from e in db.Logins
                                              where e.Username == LoginInfo
                                              select e).SingleOrDefault();
                    db.Logins.Remove(CurrentLogin);
                    db.SaveChanges();
                    LoginResponse = Login(DkClient);
                }
            }

            return LoginResponse;

        }
        
        public void SendMail(string Title, string Body)
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

            mail.From = new MailAddress("dko.errors.fl@gmail.com");
            mail.To.Add("ahfriedman09@gmail.com");
            mail.Subject = Title;
            mail.Body = Body;

            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("dko.errors.fl", "4ownershipWoes");
            SmtpServer.EnableSsl = true;

            SmtpServer.Send(mail);
        }
        public void DisposeHangfire(bool Captcha)
        {
            if (Captcha)
            {
                System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"] = "true";
            }
            else
            {
                System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"] = "true";
            }
           
        }
        public void StartHangfire(bool Captcha)
        {
            if (Captcha)
            {
                System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"] = "false";
            }
            else
            {
                System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"] = "false";
                System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"] = "false";
            }
        }
    }
}
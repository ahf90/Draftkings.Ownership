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
        public void SendLoginCreate()
        {
            Random rand = new Random();

            const string Alphabet = "abcdefghijklmnopqrstuvwxyz";
            const string Numbers = "0123456789";

            char[] chars = new char[9];
            for (int i = 0; i < chars.Length - 1; i++)
            {
                chars[i] = Alphabet[rand.Next(Alphabet.Length)];
            }
            chars[8] = Numbers[rand.Next(Numbers.Length)];
            string NewLogin = new string(chars);

            CreateLogin(NewLogin);
        }
        public void CreateLogin(string id)
        {
            Random rand = new Random();

            var client = new RestClient("https://www.draftkings.com");

            var loginRequest = new RestRequest("/account/sitelogin/true ", Method.POST);
            loginRequest.AddParameter("ExistingUserRadio", "false");
            loginRequest.AddParameter("ShowCaptcha", "False");
            loginRequest.AddParameter("RecaptchaResponse", "");
            loginRequest.AddParameter("LocaleId", 1);
            loginRequest.AddParameter("Username", id);
            loginRequest.AddParameter("Password", id);
            loginRequest.AddParameter("Email", id + "@gmail.com");
            loginRequest.AddParameter("RegPassword", id);
            loginRequest.AddParameter("ConfirmPassword", id);
            loginRequest.AddParameter("CountryId", 1);
            loginRequest.AddParameter("State", "MA");
            loginRequest.AddParameter("DobMonth", rand.Next(10, 12));
            loginRequest.AddParameter("DobDay", rand.Next(10, 28));
            loginRequest.AddParameter("DobYear", rand.Next(1982, 1996));
            loginRequest.AddParameter("AgreeTermsCondsPriv", "true");
            loginRequest.AddParameter("AgreeTermsCondsPriv", "false");
            loginRequest.AddParameter("Agree18AgeMin", "true");
            loginRequest.AddParameter("Agree18AgeMin", "false");
            loginRequest.AddParameter("RegisterForEmails", "true");
            loginRequest.AddParameter("RegisterForEmails", "false");

            loginRequest.AddHeader("content-type", "application/x-www-form-urlencoded");
            IRestResponse response = client.Execute(loginRequest);

            LoginInfo NewLoginInfo = new LoginInfo();
            NewLoginInfo.Username = id;
            NewLoginInfo.Password = id;
            NewLoginInfo.Created = DateTime.Now;
            Database.Logins.Add(NewLoginInfo);
            Database.SaveChanges();
        }
        public void StartLoginCreate()
        {
            RecurringJob.AddOrUpdate(
                () => SendLoginCreate(),
                Cron.Daily);
            //Send CRON job to create a login daily
        }
    }
}
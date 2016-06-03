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
    public class ManualController : Controller
    {
        private static readonly List<string> LoginList = new List<string> { "num2sharkfin", "num2sharkfin", "num2sharkfin", "num2sharkfin", "num2sharkfin", "num2sharkfin" };
        private ScrapeController ScrapeControllerInstance = new ScrapeController();
        // GET: Manual
        public void Entry(int id)
        {
            var DkClient = new RestClient("https://www.draftkings.com");
            IRestResponse DkResponse = Login(DkClient);

            RestRequest LoginRequestTwo = new RestRequest("contest/gamecenter/", Method.GET);
            foreach (var cookie in DkResponse.Cookies)
            {
                LoginRequestTwo.AddCookie(cookie.Name, cookie.Value);  //this adds every cookie in the previous response.
            }

            BackgroundJob.Enqueue(() => ScrapeControllerInstance.FetchContestEntryIds(LoginRequestTwo, DkClient, id));
        }
        public void Ownership(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.GetContestOwnership(id));
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
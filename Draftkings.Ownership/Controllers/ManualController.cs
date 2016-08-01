using System.Web.Mvc;
using Draftkings.Ownership.Models;
using Hangfire;
using System.Linq;
using System;

namespace Draftkings.Ownership.Controllers
{
    public class ManualController : Controller
    {
        private ScrapeController ScrapeControllerInstance = new ScrapeController();
        private LobbyController LobbyControllerInstance = new LobbyController();
        private FantasyContestsDBContextDk db = new FantasyContestsDBContextDk();

        // GET: Manual
        public void Entry(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.FetchSingular(id));
        }
        //Ownership
        public void Ownership(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.GetContestOwnership(id));
        }
        //GroupOwnership
        public void GO(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.GetOwnership(id));
        }
        //GroupContestEntriesFetch
        public void GF(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.ContestGroupFetchEntryIds(id));
        }
        //All
        public void All(int id)
        {
            BackgroundJob.Enqueue(() => MCS(id, true));
            BackgroundJob.Schedule(() => ScrapeControllerInstance.ContestGroupFetchEntryIds(id), TimeSpan.FromMinutes(1));
            BackgroundJob.Schedule(() => ScrapeControllerInstance.GetOwnership(id), TimeSpan.FromMinutes(3));
        }
        // ManualContestsSelect
        public void MCS(int id, bool old = true)
        {
            bool InitalScrape = true;
            if (!old)
            {
                InitalScrape = false;
            }
            var ContestsQuery = (from Contest in db.Contests
                                where Contest.ContestGroupId == id
                                orderby Contest.Size descending
                                select Contest).ToList();
            int TournamentCount = 0;
            int MultiplierCount = 0;
            foreach (Contest CurrentContest in ContestsQuery)
            {
                bool AddStatus = false;
                if (TournamentCount >= 2 && MultiplierCount >= 2)
                {
                    break;
                }
                if (CurrentContest.EntryFee > 1)
                {

                    if (CurrentContest.ContestTitle.IndexOf("Double Up") != -1 || CurrentContest.ContestTitle.IndexOf("50/50") != -1)
                    {
                        if (MultiplierCount < 2)
                        {
                            AddStatus = true;
                            MultiplierCount += 1;
                        }
                    }
                    else
                    {
                        if (TournamentCount < 2)
                        {
                            AddStatus = true;
                            TournamentCount += 1;
                        }
                    }
                    if (AddStatus == true)
                    {
                        bool ContestScrapeStatusExists = db.ScrapeStatuses.Any(ccp => ccp.ContestId == CurrentContest.ContestId);

                        if (!ContestScrapeStatusExists)
                        {
                            ContestScrapeStatus NewContestStatus = new ContestScrapeStatus();
                            NewContestStatus.ContestId = CurrentContest.ContestId;
                            NewContestStatus.ContestGroupId = CurrentContest.ContestGroupId;
                            NewContestStatus.InitialEntryIdScrape = InitalScrape;
                            NewContestStatus.FinalEntryIdScrape = false;
                            db.ScrapeStatuses.Add(NewContestStatus);
                            db.SaveChanges();
                            BackgroundJob.Enqueue(() => LobbyControllerInstance.CreateContestPlayers(CurrentContest.ContestId));
                        }
                    }
                }
            }
        }
        public void Stop()
        {
            System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"] = "true";
        }
        public void Start()
        {
            System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"] = "false";
            System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"] = "false";
        }

    }
}
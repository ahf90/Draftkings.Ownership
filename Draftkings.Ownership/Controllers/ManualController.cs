using System.Web.Mvc;
using Draftkings.Ownership.Models;
using Hangfire;

namespace Draftkings.Ownership.Controllers
{
    public class ManualController : Controller
    {
        private ScrapeController ScrapeControllerInstance = new ScrapeController();
        private FantasyContestsDBContextDk db = new FantasyContestsDBContextDk();

        // GET: Manual
        public void Entry(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.FetchSingular(id));
        }
        public void Ownership(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.GetContestOwnership(id));
        }
        public void Stop()
        {
            
            System.Configuration.ConfigurationManager.AppSettings["HangfirePauseFlag"] = "true";
        }
        public void Start()
        {
            System.Configuration.ConfigurationManager.AppSettings["HangfirePauseFlag"] = "false";
        }
    }
}
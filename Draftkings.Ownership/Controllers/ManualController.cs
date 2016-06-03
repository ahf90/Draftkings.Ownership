using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Hangfire;
namespace Draftkings.Ownership.Controllers
{
    public class ManualController : Controller
    {
        private ScrapeController ScrapeControllerInstance = new ScrapeController();
        // GET: Manual
        public void Entry(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.FetchEntryIds(id));
        }
        public void Ownership(int id)
        {
            BackgroundJob.Enqueue(() => ScrapeControllerInstance.GetOwnership(id));
        }
    }
}
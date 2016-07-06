using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Draftkings.Ownership.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string HangfireStatus = System.Configuration.ConfigurationManager.AppSettings["HangfirePauseFlag"];
            if (HangfireStatus == "true")
            {
                ViewBag.Message = "Tasks currently paused.";
                ViewBag.Link = "Start";
            } else
            {
                ViewBag.Message = "Tasks currently running.";
                ViewBag.Link = "Stop";
            }
            
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
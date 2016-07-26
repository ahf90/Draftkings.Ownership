using System.Web.Mvc;

namespace Draftkings.Ownership.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            string CaptchaFlag = System.Configuration.ConfigurationManager.AppSettings["CaptchaFlag"];
            string AccessDeniedFlag = System.Configuration.ConfigurationManager.AppSettings["AccessDeniedFlag"];
            if (CaptchaFlag == "true" && AccessDeniedFlag == "true")
            {
                ViewBag.Message = "Tasks currently paused because of 403 & CAPTCHA errors.";
                ViewBag.Link = "Start";
            } else if (CaptchaFlag == "true" && AccessDeniedFlag == "false")
            {
                ViewBag.Message = "Tasks currently paused because of CAPTCHA errors.";
                ViewBag.Link = "Start";
            }
            else if (CaptchaFlag == "false" && AccessDeniedFlag == "true")
            {
                ViewBag.Message = "Tasks currently paused because of 403 errors.";
                ViewBag.Link = "Start";
            }
            else
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
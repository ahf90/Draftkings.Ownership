using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Draftkings.Ownership.Models;

namespace Draftkings.Ownership.Controllers
{
    public class ContestScrapeStatusController : Controller
    {
        private FantasyContestsDBContextDk Database = new FantasyContestsDBContextDk();
        // GET: ContestScrapeStatus
        public ActionResult Index(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var ContestsQuery = from ContestStatus in Database.ScrapeStatuses
                                where ContestStatus.ContestGroupId == id
                                select ContestStatus;
            if (ContestsQuery == null)
            {
                return HttpNotFound();
            }
            return View(ContestsQuery);
        }

        // GET: ContestScrapeStatus/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ContestScrapeStatus/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContestScrapeStatus/Create
        [HttpPost]
        public ActionResult Create2(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        [HttpPost]
        public ActionResult Create([Bind(Include = "ContestId,ContestGroupId,InitialEntryIdScrape,FinalEntryIdScrape,IsOwnershipCollected")] ContestScrapeStatus scrapeStatus)
        {
            if (ModelState.IsValid)
            {
                Database.ScrapeStatuses.Add(scrapeStatus);
                Database.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(scrapeStatus);
        }

        // GET: ContestScrapeStatus/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ContestScrapeStatus/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: ContestScrapeStatus/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ContestScrapeStatus/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}

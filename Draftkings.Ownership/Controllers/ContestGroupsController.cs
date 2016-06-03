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
    public class ContestGroupsController : Controller
    {
        private FantasyContestsDBContextDk db = new FantasyContestsDBContextDk();

        // GET: ContestGroups
        public ActionResult Index(string day, string month, string year)
        {
            if (year == null)
            {
                year = "2016";
            }
            string DateString = month + "-" + day + "-" + year;
            DateTime CgDate = Convert.ToDateTime(DateString);
            DateTime CgDateEnd = CgDate.AddDays(1);
            var ContestGroupQuery = from ContestGroup in db.ContestGroups
                                    where ContestGroup.ContestStartDate >= CgDate && ContestGroup.ContestStartDate <= CgDateEnd
                                    select ContestGroup;
            return View(ContestGroupQuery);
        }

        // GET: ContestGroups/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ContestGroup contestGroup = db.ContestGroups.Find(id);
            if (contestGroup == null)
            {
                return HttpNotFound();
            }
            return View(contestGroup);
        }

        // GET: ContestGroups/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContestGroups/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ContestGroupId,SourceId,SportId,ContestGroupName,DraftGroupId,ContestStartDate,GameCount,ActiveFlag,ContestSortOrder")] ContestGroup contestGroup)
        {
            if (ModelState.IsValid)
            {
                db.ContestGroups.Add(contestGroup);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(contestGroup);
        }

        // GET: ContestGroups/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ContestGroup contestGroup = db.ContestGroups.Find(id);
            if (contestGroup == null)
            {
                return HttpNotFound();
            }
            return View(contestGroup);
        }

        // POST: ContestGroups/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ContestGroupId,SourceId,SportId,ContestGroupName,DraftGroupId,ContestStartDate,GameCount,ActiveFlag,ContestSortOrder")] ContestGroup contestGroup)
        {
            if (ModelState.IsValid)
            {
                db.Entry(contestGroup).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(contestGroup);
        }

        // GET: ContestGroups/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ContestGroup contestGroup = db.ContestGroups.Find(id);
            if (contestGroup == null)
            {
                return HttpNotFound();
            }
            return View(contestGroup);
        }

        // POST: ContestGroups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ContestGroup contestGroup = db.ContestGroups.Find(id);
            db.ContestGroups.Remove(contestGroup);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

﻿using System;
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
    public class ContestsController : Controller
    {
        private FantasyContestsDBContextDk db = new FantasyContestsDBContextDk();

        // GET: Contests
        public ActionResult Index(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var ContestsQuery = from Contest in db.Contests
                                where Contest.ContestGroupId == id
                                orderby Contest.Size descending
                                select Contest;
            if (ContestsQuery == null)
            {
                return HttpNotFound();
            }
            return View(ContestsQuery);
        }
        public ActionResult Players(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //Contest contest = db.Contests.Find(id);
            var ContestPlayersQuery = from ContestPlayers in db.ContestPlayers
                                      where ContestPlayers.ContestId == id
                                      select ContestPlayers;
            if (ContestPlayersQuery == null)
            {
                return HttpNotFound();
            }
            return View(ContestPlayersQuery);
        }

        public ActionResult Entries(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //Contest contest = db.Contests.Find(id);
            var ContestEntriesQuery = from ContestEntries in db.ContestEntries
                                      where ContestEntries.ContestId == id
                                      select ContestEntries;
            if (ContestEntriesQuery == null)
            {
                return HttpNotFound();
            }
            return View(ContestEntriesQuery);
        }
        // GET: Contests/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contest contest = db.Contests.Find(id);
            if (contest == null)
            {
                return HttpNotFound();
            }
            return View(contest);
        }

        // GET: Contests/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Contests/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ContestId,ContestGroupId,DraftGroupId,ContestTitle,ContestType,EntryCount,MultiEntry,ActiveFlag,Rake,EntryFee,Size,UserCreated,IsOwnershipCollected")] Contest contest)
        {
            if (ModelState.IsValid)
            {
                db.Contests.Add(contest);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(contest);
        }

        // GET: Contests/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contest contest = db.Contests.Find(id);
            if (contest == null)
            {
                return HttpNotFound();
            }
            return View(contest);
        }

        // POST: Contests/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ContestId,ContestGroupId,DraftGroupId,ContestTitle,ContestType,EntryCount,MultiEntry,ActiveFlag,Rake,EntryFee,Size,UserCreated,IsOwnershipCollected")] Contest contest)
        {
            if (ModelState.IsValid)
            {
                db.Entry(contest).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(contest);
        }

        // GET: Contests/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Contest contest = db.Contests.Find(id);
            if (contest == null)
            {
                return HttpNotFound();
            }
            return View(contest);
        }

        // POST: Contests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Contest contest = db.Contests.Find(id);
            db.Contests.Remove(contest);
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

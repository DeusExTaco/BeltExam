﻿using System;
using System.Diagnostics;
using System.Linq;
using ActivityCenter.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityCenter.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(MyContext context)
        {
            DbContext = context;
        }

        private MyContext DbContext { get; }

        public IActionResult Index()
        {
            return View();
        }


        [HttpPost("register")]
        public IActionResult RegisterUser(User newUser)
        {
            if (!ModelState.IsValid) return View("Index");
            if (DbContext.Users.Any(u => u.Email == newUser.Email))
            {
                ModelState.AddModelError("Email", "Email already in use!");
                return View("Index");
            }

            var hasher = new PasswordHasher<User>();
            var hash = hasher.HashPassword(newUser, newUser.Password);
            newUser.Password = hash;
            DbContext.Users.Add(newUser);
            DbContext.SaveChanges();

            HttpContext.Session.SetInt32("UserId", newUser.UserId);

            return RedirectToAction("Dashboard");
        }

        [HttpPost("login")]
        public IActionResult LoginUser(LogUser user)
        {
            if (!ModelState.IsValid) return View("Index");
            var check = DbContext.Users.FirstOrDefault(u => u.Email == user.LogEmail);

            if (check == null)
            {
                ModelState.AddModelError("LogEmail", "Invalid email or password");
                return View("Index");
            }

            var hasher = new PasswordHasher<LogUser>();
            var result = hasher.VerifyHashedPassword(user, check.Password, user.LogPassword);
            if (result == 0)
            {
                ModelState.AddModelError("LogEmail", "Invalid email or password");
                return View("Index");
            }

            HttpContext.Session.SetInt32("UserId", check.UserId);
            return RedirectToAction("Dashboard");
        }

        [HttpGet("/logout")]
        public IActionResult LogoutUser()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet("/dashboard")]
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("Index");
            var dashboardInput = DbContext
                .Events
                .Include(p => p.Participants)
                .ThenInclude(u => u.Event)
                .Include(c => c.Creator)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
            var user = HttpContext.Session.GetInt32("UserId");
            ViewBag.User = DbContext.Users.FirstOrDefault(u => u.UserId == user);
            return View(dashboardInput);
        }

        [HttpGet("/new")]
        public IActionResult NewEvent()
        {
            var user = HttpContext.Session.GetInt32("UserId");
            ViewBag.User = DbContext.Users.FirstOrDefault(u => u.UserId == user);
            return View("new");
        }

        [HttpPost("add")]
        public IActionResult AddEvent(Event newEvent)
        {
            if (ModelState.IsValid)
            {
                if (DbContext.Events.Any(e => e.EventTitle == newEvent.EventTitle))
                {
                    ModelState.AddModelError("EventTitle", "Hobbies cannot be duplicated");
                    var stuff = HttpContext.Session.GetInt32("UserId");
                    ViewBag.User = DbContext.Users.FirstOrDefault(u => u.UserId == stuff);
                    return View("New");
                }
                
                DbContext.Add(newEvent);
                DbContext.SaveChanges();
                var temp = newEvent.EventId;
                return RedirectToAction("Details", DbContext.Events.FirstOrDefault(a => a.EventId == temp));
            }

            var user = HttpContext.Session.GetInt32("UserId");
            ViewBag.User = DbContext.Users.FirstOrDefault(u => u.UserId == user);
            return View("New");
        }

        [HttpGet("edit/{eventId}")]
        public IActionResult Edit(int? eventId)
        {
            Event retrievedEvent = DbContext.Events.FirstOrDefault(e => e.EventId == eventId); 
            return View("Edit", retrievedEvent);
        }

        [HttpPost("update/{eventId}")]
        public IActionResult updateEvent(Event toUpdate, int? eventId)
        {
            Event RetrievedEvent = DbContext.Events.FirstOrDefault(e => e.EventId == eventId);
            
            if (ModelState.IsValid)
            {
                RetrievedEvent.EventTitle = toUpdate.EventTitle;
                RetrievedEvent.Description = toUpdate.Description;
                RetrievedEvent.UpdatedAt = DateTime.Now;            
                DbContext.SaveChanges();
                return RedirectToAction("Dashboard");
            }
            else
            {
                return View("Edit", RetrievedEvent);
            }
        }


        [HttpGet("/delete/{EventId}")]
        public IActionResult DeleteEvent(int? eventId)
        {
            var retrievedEvent = DbContext.Events.SingleOrDefault(e => e.EventId == eventId);
            DbContext.Events.Remove(retrievedEvent);
            DbContext.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpGet("/details/{EventId}")]
        public IActionResult Details(int? eventId)
        {
            var anEvent = DbContext
                .Events
                .Include(c => c.Creator)
                .Include(p => p.Participants)
                .ThenInclude(z => z.User)
                .FirstOrDefault(e => e.EventId == eventId);

            var user = HttpContext.Session.GetInt32("UserId");
            ViewBag.User = DbContext.Users.FirstOrDefault(u => u.UserId == user);

            return View(anEvent);
        }

        [HttpPost("/join")]
        public IActionResult JoinEvent(int eventId, int userId)
        {
            var newParticipant = new Participant
            {
                EventId = eventId,
                UserId = userId
            };
            DbContext.Participants.Add(newParticipant);
            DbContext.SaveChanges();
            return RedirectToAction("Dashboard", new {id = HttpContext.Session.GetInt32("UserId")});
        }

        [HttpPost("/leave")]
        public IActionResult LeaveEvent(int eventId, int userId)
        {
            var removeParticipant = DbContext
                .Participants
                .FirstOrDefault(p => p.UserId == userId && p.EventId == eventId);

            if (removeParticipant != null) DbContext.Participants.Remove(removeParticipant);
            DbContext.SaveChanges();
            return RedirectToAction("Dashboard", new {id = HttpContext.Session.GetInt32("UserId")});
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}
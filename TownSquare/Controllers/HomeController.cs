using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TownSquare.Data;
using TownSquare.Models;

namespace TownSquare.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var upcomingEvents = await _context.Events
            .Include(e => e.User)
            .Where(e => e.Date >= DateTime.Today)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Time)
            .Take(10)
            .ToListAsync();

        // Get RSVP counts for each event
        var eventIds = upcomingEvents.Select(e => e.Id).ToList();
        var rsvpCounts = await _context.RSVPs
            .Where(r => eventIds.Contains(r.EventId))
            .GroupBy(r => r.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var eventsWithRSVP = upcomingEvents.Select(e => new ViewModels.EventWithRSVPViewModel
        {
            Event = e,
            RSVPCount = rsvpCounts.ContainsKey(e.Id) ? rsvpCounts[e.Id] : 0
        }).ToList();

        return View(eventsWithRSVP);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
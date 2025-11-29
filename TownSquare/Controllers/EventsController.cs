using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TownSquare.Data;
using TownSquare.Models;
using TownSquare.ViewModels;
using TownSquare.Services;

namespace TownSquare.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly WeatherService _weatherService;

    public EventsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, WeatherService weatherService)
    {
        _context = context;
        _userManager = userManager;
        _weatherService = weatherService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchKeyword, string? categoryFilter, DateTime? startDate, DateTime? endDate)
    {
        var eventsQuery = _context.Events.Include(e => e.User).AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            eventsQuery = eventsQuery.Where(e =>
                e.Title.Contains(searchKeyword) ||
                e.Description.Contains(searchKeyword) ||
                e.Location.Contains(searchKeyword));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            eventsQuery = eventsQuery.Where(e => e.Category == categoryFilter);
        }

        // Apply date range filter
        if (startDate.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.Date <= endDate.Value);
        }

        var events = await eventsQuery.OrderBy(e => e.Date).ThenBy(e => e.Time).ToListAsync();
        var categories = await _context.Events.Select(e => e.Category).Distinct().OrderBy(c => c).ToListAsync();

        // Get RSVP counts for each event
        var eventIds = events.Select(e => e.Id).ToList();
        var rsvpCounts = await _context.RSVPs
            .Where(r => eventIds.Contains(r.EventId))
            .GroupBy(r => r.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var eventsWithRSVP = events.Select(e => new EventWithRSVPViewModel
        {
            Event = e,
            RSVPCount = rsvpCounts.ContainsKey(e.Id) ? rsvpCounts[e.Id] : 0
        }).ToList();

        var viewModel = new EventIndexViewModel
        {
            Events = eventsWithRSVP,
            SearchKeyword = searchKeyword,
            CategoryFilter = categoryFilter,
            StartDate = startDate,
            EndDate = endDate,
            Categories = categories
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventItem = await _context.Events
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        // Get RSVP information
        var rsvps = await _context.RSVPs
            .Include(r => r.User)
            .Where(r => r.EventId == id)
            .ToListAsync();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var currentUserRSVPd = currentUserId != null && rsvps.Any(r => r.UserId == currentUserId);

        // Get weather forecast for event date
        var weather = await _weatherService.GetWeatherForecastAsync(eventItem.Date);

        var viewModel = new EventDetailsViewModel
        {
            Event = eventItem,
            RSVPCount = rsvps.Count,
            RSVPUsers = rsvps.Select(r => r.User!).ToList(),
            CurrentUserRSVPd = currentUserRSVPd,
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            Weather = weather
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Event eventItem)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            eventItem.UserId = user.Id;
            eventItem.CreatedAt = DateTime.UtcNow;

            _context.Add(eventItem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(eventItem);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventItem = await _context.Events.FindAsync(id);
        if (eventItem == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }

        // Allow if user is admin or owns the event
        if (user.Role != "Admin" && eventItem.UserId != user.Id)
        {
            return Forbid();
        }

        return View(eventItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Event eventItem)
    {
        if (id != eventItem.Id)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }

        // Allow if user is admin or owns the event
        if (user.Role != "Admin" && eventItem.UserId != user.Id)
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(eventItem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(eventItem.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        return View(eventItem);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventItem = await _context.Events
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }

        // Allow if user is admin or owns the event
        if (user.Role != "Admin" && eventItem.UserId != user.Id)
        {
            return Forbid();
        }

        return View(eventItem);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var eventItem = await _context.Events.FindAsync(id);
        if (eventItem == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }

        // Allow if user is admin or owns the event
        if (user.Role != "Admin" && eventItem.UserId != user.Id)
        {
            return Forbid();
        }

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RSVP(int id)
    {
        var eventItem = await _context.Events.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
        if (eventItem == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Check if already RSVP'd
        var existingRSVP = await _context.RSVPs
            .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == user.Id);

        if (existingRSVP != null)
        {
            // Already RSVP'd, just redirect back
            return RedirectToAction(nameof(Details), new { id });
        }

        // Create new RSVP
        var rsvp = new RSVP
        {
            EventId = id,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.RSVPs.Add(rsvp);

        // Create notification for event creator (if not RSVPing to own event)
        if (eventItem.UserId != user.Id)
        {
            var notification = new Notification
            {
                UserId = eventItem.UserId,
                Message = $"{user.FullName} RSVP'd to your event '{eventItem.Title}'",
                EventId = id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRSVP(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var rsvp = await _context.RSVPs
            .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == user.Id);

        if (rsvp != null)
        {
            _context.RSVPs.Remove(rsvp);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private bool EventExists(int id)
    {
        return _context.Events.Any(e => e.Id == id);
    }
}

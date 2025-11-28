using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TownSquare.Data;
using TownSquare.Models;
using TownSquare.ViewModels;

namespace TownSquare.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EventsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
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

        var viewModel = new EventIndexViewModel
        {
            Events = events,
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

        return View(eventItem);
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

    private bool EventExists(int id)
    {
        return _context.Events.Any(e => e.Id == id);
    }
}

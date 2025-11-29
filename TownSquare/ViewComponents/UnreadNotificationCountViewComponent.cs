using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TownSquare.Data;
using TownSquare.Models;

namespace TownSquare.ViewComponents;

public class UnreadNotificationCountViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UnreadNotificationCountViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Content("");
        }

        var count = await _context.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .CountAsync();

        return View(count);
    }
}

using TownSquare.Models;

namespace TownSquare.ViewModels;

public class EventDetailsViewModel
{
    public Event Event { get; set; } = null!;
    public int RSVPCount { get; set; }
    public List<ApplicationUser> RSVPUsers { get; set; } = new();
    public bool CurrentUserRSVPd { get; set; }
    public bool IsAuthenticated { get; set; }
}

using TownSquare.Models;

namespace TownSquare.ViewModels;

public class EventWithRSVPViewModel
{
    public Event Event { get; set; } = null!;
    public int RSVPCount { get; set; }
}

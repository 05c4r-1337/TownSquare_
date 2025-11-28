using TownSquare.Models;

namespace TownSquare.ViewModels;

public class ProfileViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<Event> CreatedEvents { get; set; } = new();
}

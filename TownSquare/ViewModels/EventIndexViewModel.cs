namespace TownSquare.ViewModels;

public class EventIndexViewModel
{
    public List<EventWithRSVPViewModel> Events { get; set; } = new();
    public string? SearchKeyword { get; set; }
    public string? CategoryFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Categories { get; set; } = new();
}

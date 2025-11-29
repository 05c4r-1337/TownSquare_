using System.ComponentModel.DataAnnotations;

namespace TownSquare.Models;

public class RSVP
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    public Event? Event { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

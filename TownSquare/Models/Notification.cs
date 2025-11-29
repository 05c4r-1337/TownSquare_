using System.ComponentModel.DataAnnotations;

namespace TownSquare.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public int? EventId { get; set; }

    public Event? Event { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

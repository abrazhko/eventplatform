namespace EventPlatform.Models;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? EventId { get; set; }
    public Event? Event { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;
    // Reminder
    // ScheduleChange
    // Cancellation
    // PlatformNews

    public bool IsRead { get; set; } = false;

    public bool EmailSent { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
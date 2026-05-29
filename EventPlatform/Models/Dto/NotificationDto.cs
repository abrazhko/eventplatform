namespace EventPlatform.Models.Dto;

public class NotificationDto
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? EventTitle { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}
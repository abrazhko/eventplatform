namespace EventPlatform.Models
{
    public enum EventStatus
    {
        Draft = 0,
        Published = 1,
        Finished = 2,
        Cancelled = 3
    }

    public class Event
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string Location { get; set; } = null!;

        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public decimal Price { get; set; }
        public int Capacity { get; set; }

        public EventStatus Status { get; set; }

        // FK
        public int OrganizerId { get; set; }
        public User Organizer { get; set; }

        public DateTime CreatedAt { get; set; }

        // Навигация
        public ICollection<Ticket> Tickets { get; set; }
    }
}

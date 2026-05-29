namespace EventPlatform.Models
{
    public enum TicketStatus
    {
        Active = 1,
        Used = 2,
        Cancelled = 3
    }

    public class Ticket
    {
        public int Id { get; set; }

        public string QrCode { get; set; } = null!;
        public TicketStatus Status { get; set; }

        public DateTime PurchasedAt { get; set; }

        // FK
        public int EventId { get; set; }
        public Event Event { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}

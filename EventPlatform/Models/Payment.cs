namespace EventPlatform.Models
{
    public enum PaymentStatus
    {
        Pending = 1,
        Paid = 2,
        Failed = 3
    }

    public class Payment
    {
        public int Id { get; set; }

        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }

        public string Provider { get; set; } // Stripe, YooKassa
        public string TransactionId { get; set; }

        public DateTime CreatedAt { get; set; }

        // FK
        public int UserId { get; set; }
        public User User { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }
    }
}

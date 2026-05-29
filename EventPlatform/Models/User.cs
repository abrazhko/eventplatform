namespace EventPlatform.Models
{
    public enum UserRole
    {
        Admin = 1,
        Organizer = 2,
        Participant = 3
    }

    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public UserRole Role { get; set; }

        public bool IsBlocked { get; set; }
        public DateTime CreatedAt { get; set; }

        // Навигация
        public ICollection<Event> OrganizedEvents { get; set; }
        public ICollection<Ticket> Tickets { get; set; }
    }
}

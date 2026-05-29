namespace EventPlatform.Models
{
    public class AdminDashboardViewModel : BaseDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveOrganizers { get; set; }
        public int PublishedEvents { get; set; }
        public decimal MonthlyRevenue { get; set; }

        public int NewUsersThisWeek { get; set; }
        public int EventsToday { get; set; }
        public int TicketsSoldToday { get; set; }

        public List<string> Last7Days { get; set; } = new();
        public List<int> TicketsLast7Days { get; set; } = new();

        public List<Event> UpcomingEvents { get; set; } = new();
        public List<User> LatestUsers { get; set; } = new();
    }
}
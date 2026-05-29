namespace EventPlatform.Models
{
    public class OrganizerDashboardViewModel : BaseDashboardViewModel
    {
        public int OrganizerId { get; set; }

        // KPI
        public int TotalEvents { get; set; }
        public int PublishedEvents { get; set; }
        public int TotalTickets { get; set; }
        public decimal MonthlyRevenue { get; set; }

        public int TicketsToday { get; set; }

        // ближайшее событие
        public Event? NearestEvent { get; set; }

        // график (7 дней)
        public List<string> Last7Days { get; set; } = new();
        public List<int> TicketsLast7Days { get; set; } = new();

        // список мероприятий
        public List<Event> UpcomingEvents { get; set; } = new();

        // последние покупки
        public List<Ticket> LatestTickets { get; set; } = new();
    }
}
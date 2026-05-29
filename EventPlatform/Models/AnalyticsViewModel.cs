using System;
using System.Collections.Generic;

namespace EventPlatform.Models
{
    public class AnalyticsViewModel
    {
        // Filters
        public string Period { get; set; }
        public int? CategoryId { get; set; }
        public int? OrganizerId { get; set; }
        public decimal AvgTicketPrice { get; set; }

        // KPI
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalEvents { get; set; }
        public int TotalTickets { get; set; }
        public decimal TotalRevenue { get; set; }

        // Charts
        public List<string> UserGrowthLabels { get; set; } = new();
        public List<int> UserGrowthData { get; set; } = new();

        public List<string> TicketSalesLabels { get; set; } = new();
        public List<int> TicketSalesData { get; set; } = new();

        public List<string> CategoryLabels { get; set; } = new();
        public List<int> CategoryData { get; set; } = new();

        public List<string> RevenueLabels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();

        // Tables
        public List<TopOrganizerViewModel> TopOrganizers { get; set; } = new();
        public List<TopEventViewModel> TopEvents { get; set; } = new();
    }

    public class TopOrganizerViewModel
    {
        public string Organizer { get; set; }
        public int Events { get; set; }
        public int Tickets { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopEventViewModel
    {
        public string EventTitle { get; set; }
        public string Organizer { get; set; }
        public int Tickets { get; set; }
        public decimal Revenue { get; set; }
    }
}

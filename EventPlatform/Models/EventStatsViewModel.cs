namespace EventPlatform.Models
{
    public class EventStatsViewModel
    {
        public string EventTitle { get; set; }

        public int Capacity { get; set; }
        public int SoldTickets { get; set; }
        public int RemainingTickets { get; set; }

        public decimal Revenue { get; set; }

        public int FillPercent { get; set; }

        public List<string> Labels { get; set; }
        public List<int> Sales { get; set; }
    }
}
namespace ActivityTracker.Models
{
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public double TotalDistance { get; set; } // w metrach
        public int TotalDuration { get; set; } // w sekundach
        public int ActivityCount { get; set; }
    }
}
namespace ActivityTracker.Models
{
    public class LeaderboardResponse
    {
        public string Period { get; set; } = "30days";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<LeaderboardEntry> Entries { get; set; } = new();
        public LeaderboardEntry? CurrentUserEntry { get; set; }
    }
}
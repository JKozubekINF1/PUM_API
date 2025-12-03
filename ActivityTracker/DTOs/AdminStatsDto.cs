namespace ActivityTracker.DTOs
{
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalActivities { get; set; }
        public double TotalDistanceKm { get; set; }
        public double TotalDurationHours { get; set; }
    }
}
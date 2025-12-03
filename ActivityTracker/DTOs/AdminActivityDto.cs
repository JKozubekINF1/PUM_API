namespace ActivityTracker.DTOs
{
    public class AdminActivityDto
    {
        public Guid Id { get; set; }

        // Dane użytkownika
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string? UserAvatarUrl { get; set; }

        // Dane aktywności
        public string Title { get; set; }
        public string ActivityType { get; set; }
        public double DistanceMeters { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
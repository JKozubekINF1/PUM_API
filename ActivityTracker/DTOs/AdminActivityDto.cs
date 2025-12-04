namespace ActivityTracker.DTOs
{
    public class AdminActivityDto
    {
        public Guid Id { get; set; }

        // Dane użytkownika (do wyświetlania w nagłówku karty)
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public string? UserAvatarUrl { get; set; }

        // Dane aktywności podstawowe (do tabeli)
        public string? Title { get; set; }
        public required string ActivityType { get; set; }
        public double DistanceMeters { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime StartedAt { get; set; }

        // --- NOWE POLA SZCZEGÓŁOWE (do modala) ---
        public DateTime EndedAt { get; set; }
        public double AverageSpeedMs { get; set; }
        public double? MaxSpeedMs { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace ActivityTracker.Models
{
    public class Activity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = null!;

        public string Title { get; set; } = "Bez tytułu";  
        public string? Description { get; set; }          

        public string ActivityType { get; set; } = "Running"; 

        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }

        public int DurationSeconds { get; set; }
        public double DistanceMeters { get; set; }
        public double AverageSpeedMs { get; set; }
        public double? MaxSpeedMs { get; set; }

        public string? RouteGeoJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
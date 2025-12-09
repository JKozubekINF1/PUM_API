namespace ActivityTracker.DTOs;

public class ProfileDto
{
    public string? UserName { get; set; } 
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }
    public string? AvatarUrl { get; set; }

    public double TotalDistanceKm { get; set; }
    public int TotalActivities { get; set; }
    public double TotalDurationSeconds { get; set; }
}
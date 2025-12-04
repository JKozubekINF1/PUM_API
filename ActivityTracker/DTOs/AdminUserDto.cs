namespace ActivityTracker.DTOs
{
    public class AdminUserDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public int ActivitiesCount { get; set; }

        // --- NOWE POLA SZCZEGÓŁOWE ---
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public double? Height { get; set; } 
        public double? Weight { get; set; } 
    }
}
using Microsoft.AspNetCore.Identity;

namespace ActivityTracker.Models;


public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; } 
    public double? Height { get; set; } 
    public double? Weight { get; set; } 
    public string? AvatarUrl { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}


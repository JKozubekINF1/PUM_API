using System.ComponentModel.DataAnnotations;

namespace ActivityTracker.DTOs;

public class UpdateProfileDto
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [Range(0, 300)]
    public double? Height { get; set; }

    [Range(0, 500)]
    public double? Weight { get; set; }

    [Url]
    public string? AvatarUrl { get; set; }
}


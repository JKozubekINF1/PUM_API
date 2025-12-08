using System.ComponentModel.DataAnnotations;

namespace ActivityTracker.DTOs;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
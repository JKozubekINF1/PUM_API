using System.ComponentModel.DataAnnotations;

namespace ActivityTracker.DTOs;

public class ChangePasswordDto
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
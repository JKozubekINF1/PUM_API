using System.ComponentModel.DataAnnotations;

namespace ActivityTracker.DTOs;

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty; // Token, który przyjdzie mailem

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare("NewPassword", ErrorMessage = "Hasła nie są identyczne.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
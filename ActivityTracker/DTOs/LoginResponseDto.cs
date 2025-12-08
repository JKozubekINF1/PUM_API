namespace ActivityTracker.DTOs;

public class LoginResponseDto
{
    public required string Token { get; set; }
    public required string Email { get; set; }
    public bool MustChangePassword { get; set; }

}



using ActivityTracker.DTOs;
using ActivityTracker.Models;
using ActivityTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActivityTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;
    private readonly IEmailService _emailService; 

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtService jwtService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        if (registerDto.Password != registerDto.ConfirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        var existingUserByName = await _userManager.FindByNameAsync(registerDto.UserName);
        if (existingUserByName != null)
        {
            return BadRequest("This username is already taken.");
        }

        var existingUserByEmail = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUserByEmail != null)
        {
            return BadRequest("This email is already registered.");
        }

        var user = new ApplicationUser
        {
            UserName = registerDto.UserName,
            Email = registerDto.Email
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            return Ok(new { Message = "User registered successfully!" });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);

        if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            var token = await _jwtService.GenerateTokenAsync(user);

            // ZWRACAMY INFORMACJĘ O KONIECZNOŚCI ZMIANY HASŁA
            return Ok(new LoginResponseDto
            {
                Token = token,
                Email = user.Email!,
                MustChangePassword = user.MustChangePassword
            });
        }
        return Unauthorized("Invalid credentials.");
    }

    [HttpPost("change-initial-password")]
    [Authorize] // Musi być zalogowany (mieć token), żeby zmienić hasło
    public async Task<IActionResult> ChangeInitialPassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null) return Unauthorized();

        if (!user.MustChangePassword)
        {
            return BadRequest("Nie musisz zmieniać hasła.");
        }

        // Generujemy token resetujący hasło (bezpieczna metoda bez podawania starego hasła, bo user jest już zalogowany)
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

        if (result.Succeeded)
        {
            // Wyłączamy wymóg zmiany hasła
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
            return Ok(new { Message = "Hasło zostało zmienione." });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            return Ok(new { Message = "Jeśli podany email istnieje w naszej bazie, wysłaliśmy instrukcję resetowania hasła." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var subject = "Reset hasła - Activity Tracker";
        var body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                <h2>Cześć {user.UserName}!</h2>
                <p>Otrzymaliśmy prośbę o zresetowanie hasła do Twojego konta.</p>
                <p>Twój kod resetujący to:</p>
                <div style='background-color: #f4f4f4; padding: 15px; border-radius: 5px; display: inline-block;'>
                    <h2 style='margin: 0; letter-spacing: 3px; color: #007bff;'>{token}</h2>
                </div>
                <p>Skopiuj ten kod i wklej go w aplikacji, aby ustawić nowe hasło.</p>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                <small style='color: #888;'>Jeśli to nie Ty wysłałeś prośbę, zignoruj tę wiadomość.</small>
            </div>";

        try
        {
            await _emailService.SendEmailAsync(user.Email!, subject, body);
        }
        catch (Exception ex)
        {
            return BadRequest($"Wystąpił błąd podczas wysyłania e-maila. Sprawdź konfigurację SMTP. Szczegóły: {ex.Message}");
        }

        return Ok(new { Message = "Jeśli podany email istnieje w naszej bazie, wysłaliśmy instrukcję resetowania hasła." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmNewPassword)
        {
            return BadRequest("Hasła nie są identyczne.");
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return BadRequest("Nieprawidłowy adres email.");
        }

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

        if (result.Succeeded)
        {
            return Ok(new { Message = "Hasło zostało pomyślnie zmienione. Możesz się teraz zalogować." });
        }

        return BadRequest(result.Errors);
    }

}
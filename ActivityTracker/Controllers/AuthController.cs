using ActivityTracker.DTOs;
using ActivityTracker.Models;
using ActivityTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ActivityTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;

    public AuthController(UserManager<ApplicationUser> userManager, JwtService jwtService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
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
            return Ok(new LoginResponseDto { Token = token, Email = user.Email! });
        }

        return Unauthorized("Invalid credentials.");
    }
}
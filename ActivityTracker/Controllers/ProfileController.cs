using System.Security.Claims;
using ActivityTracker.DTOs;
using ActivityTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ActivityTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }
        return await _userManager.FindByIdAsync(userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var profileDto = new ProfileDto
        {
            UserName = user.UserName, 
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            Height = user.Height,
            Weight = user.Weight,
            AvatarUrl = user.AvatarUrl
        };

        return Ok(profileDto);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(updateProfileDto.UserName) && updateProfileDto.UserName != user.UserName)
        {
            var existingUser = await _userManager.FindByNameAsync(updateProfileDto.UserName);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return BadRequest("Ten nick jest już zajęty.");
            }


            user.UserName = updateProfileDto.UserName;
        }
        
        user.FirstName = updateProfileDto.FirstName ?? user.FirstName;
        user.LastName = updateProfileDto.LastName ?? user.LastName;
        user.DateOfBirth = updateProfileDto.DateOfBirth ?? user.DateOfBirth;
        user.Gender = updateProfileDto.Gender ?? user.Gender;
        user.Height = updateProfileDto.Height ?? user.Height;
        user.Weight = updateProfileDto.Weight ?? user.Weight;
        user.AvatarUrl = updateProfileDto.AvatarUrl ?? user.AvatarUrl;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            return Ok(new { Message = "Profile updated successfully." });
        }

        return BadRequest(result.Errors);
    }
}
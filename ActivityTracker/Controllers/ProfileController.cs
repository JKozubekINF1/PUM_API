using ActivityTracker.Data;
using ActivityTracker.DTOs;
using ActivityTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ActivityTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _db;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        ApplicationDbContext db) 
    {
        _userManager = userManager;
        _environment = environment;
        _db = db; 
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

        var stats = await _db.Activities
            .Where(a => a.UserId == user.Id)
            .GroupBy(x => 1) 
            .Select(g => new
            {
                TotalDistance = g.Sum(x => x.DistanceMeters),
                TotalDuration = g.Sum(x => x.DurationSeconds),
                Count = g.Count()
            })
            .FirstOrDefaultAsync();

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
            AvatarUrl = user.AvatarUrl,
            TotalDistanceKm = stats != null ? Math.Round(stats.TotalDistance / 1000.0, 2) : 0,
            TotalActivities = stats?.Count ?? 0,
            TotalDurationSeconds = stats?.TotalDuration ?? 0
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

    [HttpPost("upload-avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound("User not found.");

        if (file == null || file.Length == 0)
            return BadRequest("Nie przesłano pliku.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Tylko pliki .jpg, .jpeg, .png są dozwolone.");

        string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        string uniqueFileName = $"{user.Id}_{Guid.NewGuid()}{extension}";
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        var avatarUrl = $"{Request.Scheme}://{Request.Host}/uploads/avatars/{uniqueFileName}";

        user.AvatarUrl = avatarUrl;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            return Ok(new { Url = avatarUrl, Message = "Awatar zaktualizowany." });
        }

        return BadRequest("Błąd zapisu w bazie danych.");
    }
}
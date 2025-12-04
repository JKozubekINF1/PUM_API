using ActivityTracker.Data;
using ActivityTracker.DTOs;
using ActivityTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ActivityTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // 1. Lista użytkowników + licznik aktywności
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsers()
        {
            var users = await _db.Users
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    UserName = u.UserName ?? "Nieznany",
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AvatarUrl = u.AvatarUrl,
                    ActivitiesCount = u.Activities.Count(),
                    PhoneNumber = u.PhoneNumber,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender,
                    Height = u.Height,
                    Weight = u.Weight
                })
                .ToListAsync();

            return Ok(users);
        }

        // 2. USUWANIE UŻYTKOWNIKA
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Użytkownik nie istnieje.");
            }

            if (User.Identity?.Name == user.UserName)
            {
                return BadRequest("Nie możesz usunąć własnego konta administratora.");
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest("Wystąpił błąd podczas usuwania użytkownika.");
            }

            return NoContent();
        }

        // 3. Lista aktywności z filtrowaniem
        [HttpGet("activities")]
        public async Task<ActionResult<IEnumerable<AdminActivityDto>>> GetActivities(
            [FromQuery] string? userId,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? activityType,
            [FromQuery] double? minDistance)
        {
            var query = from a in _db.Activities
                        join u in _db.Users on a.UserId equals u.Id
                        select new { Activity = a, User = u };

            // Filtrowanie (bez zmian)
            if (!string.IsNullOrEmpty(userId)) query = query.Where(x => x.Activity.UserId == userId);
            if (dateFrom.HasValue) query = query.Where(x => x.Activity.StartedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(x => x.Activity.StartedAt <= dateTo.Value);
            if (!string.IsNullOrEmpty(activityType)) query = query.Where(x => x.Activity.ActivityType == activityType);
            if (minDistance.HasValue) query = query.Where(x => x.Activity.DistanceMeters >= minDistance.Value);

            // Sortowanie
            query = query.OrderByDescending(x => x.Activity.StartedAt);

            // Mapowanie na nowe DTO
            var result = await query.Select(x => new AdminActivityDto
            {
                Id = x.Activity.Id,
                UserId = x.User.Id,
                UserName = x.User.UserName ?? "Nieznany",
                UserAvatarUrl = x.User.AvatarUrl,

                Title = x.Activity.Title,
                ActivityType = x.Activity.ActivityType,
                DistanceMeters = x.Activity.DistanceMeters,
                DurationSeconds = x.Activity.DurationSeconds,
                StartedAt = x.Activity.StartedAt,

                // Nowe pola
                EndedAt = x.Activity.EndedAt,
                AverageSpeedMs = x.Activity.AverageSpeedMs,
                MaxSpeedMs = x.Activity.MaxSpeedMs,
                Description = x.Activity.Description,
                CreatedAt = x.Activity.CreatedAt
            }).ToListAsync();

            return Ok(result);
        }

        // 4. Usuwanie aktywności
        [HttpDelete("activities/{id}")]
        public async Task<IActionResult> DeleteActivity(Guid id)
        {
            var activity = await _db.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound("Nie znaleziono aktywności.");
            }

            _db.Activities.Remove(activity);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // 5. Statystyki globalne
        [HttpGet("stats")]
        public async Task<ActionResult<AdminStatsDto>> GetStatistics()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalActivities = await _db.Activities.CountAsync();

            var totalDistance = await _db.Activities.SumAsync(a => (double?)a.DistanceMeters) ?? 0;
            var totalDuration = await _db.Activities.SumAsync(a => (int?)a.DurationSeconds) ?? 0;

            return Ok(new AdminStatsDto
            {
                TotalUsers = totalUsers,
                TotalActivities = totalActivities,
                TotalDistanceKm = Math.Round(totalDistance / 1000.0, 2), // Konwersja na km
                TotalDurationHours = Math.Round(totalDuration / 3600.0, 2) // Konwersja na godziny
            });
        }
    }
}
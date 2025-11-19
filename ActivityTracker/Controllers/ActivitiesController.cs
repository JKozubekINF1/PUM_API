using ActivityTracker.Data;
using ActivityTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Security.Claims;

namespace ActivityTracker.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ActivitiesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveActivityRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            string? geoJson = null;
            if (request.Route?.Count >= 2)
            {
                var coordinates = request.Route
                    .Select(p => new Coordinate(p[0], p[1]))
                    .ToArray();

                var factory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(4326);
                var lineString = factory.CreateLineString(coordinates);
                geoJson = new GeoJsonWriter().Write(lineString);
            }

            var activity = new Activity
            {
                UserId = userId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? "Bez tytułu" : request.Title.Trim(),     
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(), 
                ActivityType = request.ActivityType ?? "Running",
                StartedAt = DateTime.UtcNow.AddSeconds(-request.DurationSeconds),
                EndedAt = DateTime.UtcNow,
                DurationSeconds = request.DurationSeconds,
                DistanceMeters = request.DistanceMeters,
                AverageSpeedMs = request.AverageSpeedMs,
                MaxSpeedMs = request.MaxSpeedMs,
                RouteGeoJson = geoJson
            };

            _db.Entry(activity).Property("UserId").CurrentValue = userId;

            _db.Activities.Add(activity);
            await _db.SaveChangesAsync();

            return Ok(new { Id = activity.Id, Message = "Activity saved successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyActivities()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var activities = await _db.Activities
                .Where(a => EF.Property<string>(a, "UserId") == userId)
                .OrderByDescending(a => a.StartedAt)
                .Select(a => new
                {
                    a.Id,
                    a.ActivityType,
                    a.StartedAt,
                    a.DurationSeconds,
                    a.DistanceMeters,
                    a.AverageSpeedMs,
                    a.RouteGeoJson
                })
                .ToListAsync();

            return Ok(activities);
        }
    }

    public class SaveActivityRequest
    {
        public string? Title { get; set; }          
        public string? Description { get; set; }     
        public string? ActivityType { get; set; }
        public int DurationSeconds { get; set; }
        public double DistanceMeters { get; set; }
        public double AverageSpeedMs { get; set; }
        public double? MaxSpeedMs { get; set; }
        public List<List<double>>? Route { get; set; }
    }
}
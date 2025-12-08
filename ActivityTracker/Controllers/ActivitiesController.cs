using ActivityTracker.Data;
using ActivityTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Security.Claims;
using System.Text;
using System.Globalization;

namespace ActivityTracker.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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

            _db.Activities.Add(activity);
            await _db.SaveChangesAsync();

            return Ok(new { Id = activity.Id, Message = "Activity saved successfully!" });
        }


        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var activities = await _db.Activities
                .Where(a => a.UserId == userId) 
                .OrderByDescending(a => a.StartedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.ActivityType,
                    a.StartedAt,
                    a.DurationSeconds,
                    a.DistanceMeters,
                    a.AverageSpeedMs,

                })
                .ToListAsync();

            return Ok(activities);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetActivityDetails(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var activity = await _db.Activities
                .Where(a => a.Id == id && a.UserId == userId) 
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.ActivityType,
                    a.StartedAt,
                    a.EndedAt,
                    a.DurationSeconds,
                    a.DistanceMeters,
                    a.AverageSpeedMs,
                    a.MaxSpeedMs,
                    a.RouteGeoJson 
                })
                .FirstOrDefaultAsync();

            if (activity == null)
                return NotFound("Nie znaleziono aktywności.");

            return Ok(activity);
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLeaderboard()
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var rankingData = await _db.Activities
                .Where(a => a.StartedAt >= sevenDaysAgo)
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalDistance = g.Sum(x => x.DistanceMeters),
                    TotalDuration = g.Sum(x => x.DurationSeconds),
                    ActivityCount = g.Count()
                })
                .OrderByDescending(x => x.TotalDistance)
                .Take(50)
                .ToListAsync();

            var userIds = rankingData.Select(r => r.UserId).ToList();

            var users = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.FirstName, u.LastName, u.AvatarUrl })
                .ToDictionaryAsync(u => u.Id);

            var leaderboard = rankingData.Select((r, index) => {
                var userExists = users.TryGetValue(r.UserId, out var user);

                string displayName = "Anonim";
                if (userExists)
                {
                    if (!string.IsNullOrEmpty(user.FirstName))
                        displayName = user.FirstName + (string.IsNullOrEmpty(user.LastName) ? "" : " " + user.LastName);
                    else
                        displayName = user.UserName ?? "Użytkownik";
                }

                return new
                {
                    Rank = index + 1,
                    UserName = displayName,
                    AvatarUrl = userExists ? user.AvatarUrl : null,
                    TotalDistanceKm = Math.Round(r.TotalDistance / 1000.0, 2),
                    r.ActivityCount,
                    r.TotalDuration
                };
            });

            return Ok(leaderboard);
        }

        [HttpGet("{id}/gpx")]
        public async Task<IActionResult> ExportGpx(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var activity = await _db.Activities
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (activity == null)
            {
                return NotFound("Nie znaleziono aktywności.");
            }

            if (string.IsNullOrEmpty(activity.RouteGeoJson))
            {
                return BadRequest("Ta aktywność nie posiada zapisanej trasy GPS.");
            }

            var reader = new GeoJsonReader();
            var geometry = reader.Read<Geometry>(activity.RouteGeoJson);

            var coordinates = geometry.Coordinates;

            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<gpx version=\"1.1\" creator=\"ActivityTrackerAPI\" xmlns=\"http://www.topografix.com/GPX/1/1\">");

            sb.AppendLine("  <metadata>");
            sb.AppendLine($"    <time>{activity.StartedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")}</time>");
            sb.AppendLine("  </metadata>");

            sb.AppendLine("  <trk>");
            sb.AppendLine($"    <name>{activity.Title ?? "Activity"}</name>");
            sb.AppendLine($"    <type>{activity.ActivityType}</type>");
            sb.AppendLine("    <trkseg>");

            foreach (var coord in coordinates)
            {
                string lat = coord.Y.ToString(CultureInfo.InvariantCulture); 
                string lon = coord.X.ToString(CultureInfo.InvariantCulture); 

                sb.AppendLine($"      <trkpt lat=\"{lat}\" lon=\"{lon}\">");
                sb.AppendLine("      </trkpt>");
            }

            sb.AppendLine("    </trkseg>");
            sb.AppendLine("  </trk>");
            sb.AppendLine("</gpx>");

            var fileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"{activity.ActivityType}_{activity.StartedAt:yyyy-MM-dd}.gpx";

            return File(fileBytes, "application/gpx+xml", fileName);
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
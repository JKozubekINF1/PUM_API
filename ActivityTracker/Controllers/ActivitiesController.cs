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
        private readonly IWebHostEnvironment _environment;

        public ActivitiesController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment) 
        {
            _db = db;
            _userManager = userManager;
            _environment = environment;
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
                    a.PhotoUrl 
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
                    a.RouteGeoJson,
                    a.PhotoUrl 
                })
                .FirstOrDefaultAsync();

            if (activity == null)
                return NotFound("Nie znaleziono aktywności.");

            return Ok(activity);
        }

        [HttpPost("{id}/photo")]
        public async Task<IActionResult> UploadActivityPhoto(Guid id, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var activity = await _db.Activities
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (activity == null)
                return NotFound("Nie znaleziono aktywności lub brak dostępu.");

            if (file == null || file.Length == 0)
                return BadRequest("Nie przesłano pliku.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Tylko pliki .jpg, .jpeg, .png są dozwolone.");

            string rootPath = _environment.WebRootPath ?? _environment.ContentRootPath;

            string uploadsFolder = Path.Combine(rootPath, _environment.WebRootPath == null ? "wwwroot" : "", "uploads", "activity-photos");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = $"{activity.Id}_{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photoUrl = $"{Request.Scheme}://{Request.Host}/uploads/activity-photos/{uniqueFileName}";

            activity.PhotoUrl = photoUrl;
            _db.Activities.Update(activity);
            await _db.SaveChangesAsync();

            return Ok(new { Url = photoUrl, Message = "Zdjęcie aktywności dodane pomyślnie." });
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

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            var startDate = DateTime.UtcNow.AddDays(-30);

            var leaderboard = await _db.Activities
                .Where(a => a.StartedAt >= startDate)
                .Join(_db.Users,
                    activity => activity.UserId,
                    user => user.Id,
                    (activity, user) => new { activity, user })
                .GroupBy(x => new { x.user.Id, x.user.UserName })
                .Select(g => new
                {
                    UserName = g.Key.UserName ?? "User",
                    TotalDistanceKm = Math.Round(g.Sum(x => x.activity.DistanceMeters) / 1000, 1),
                    ActivityCount = g.Count()
                })
                .OrderByDescending(x => x.TotalDistanceKm)
                .Take(50)
                .ToListAsync();

            var result = leaderboard.Select((item, index) => new
            {
                Position = index + 1,
                item.UserName,
                item.TotalDistanceKm,
                item.ActivityCount
            }).ToList();

            return Ok(new
            {
                Period = "30 dni",
                Updated = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Ranking = result
            });
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
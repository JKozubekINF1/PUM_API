#nullable disable
using ActivityTracker.Controllers;
using ActivityTracker.Data;
using ActivityTracker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace ActivityTracker.Tests
{
    public class ActivitiesTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly ActivitiesController _controller;

        private const string TestUserId = "user-guid-123";

        public ActivitiesTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            _mockUserManager = MockUserManager<ApplicationUser>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(x => x.WebRootPath).Returns("wwwroot");
            _controller = new ActivitiesController(
                _dbContext,
                _mockUserManager.Object,
                _mockEnvironment.Object
            );

            SetupUserContext(TestUserId, "TestUser");
        }

        [Fact]
        public async Task Save_AddsActivityToDatabase_AndReturnsOk()
        {
            var request = new SaveActivityRequest
            {
                Title = "Morning Run",
                ActivityType = "Running",
                DistanceMeters = 5000,
                DurationSeconds = 1800,
                Route = new List<List<double>>
                {
                    new List<double> { 50.0, 19.0 },
                    new List<double> { 50.1, 19.1 }
                }
            };

            var result = await _controller.Save(request);

            Assert.IsType<OkObjectResult>(result);

            var activityInDb = await _dbContext.Activities.FirstOrDefaultAsync();
            Assert.NotNull(activityInDb);
            Assert.Equal("Morning Run", activityInDb.Title);
            Assert.Equal(TestUserId, activityInDb.UserId);
            Assert.NotNull(activityInDb.RouteGeoJson);
        }


        [Fact]
        public async Task GetHistory_ReturnsOnlyUserActivities()
        {
            _dbContext.Activities.Add(new Activity { UserId = TestUserId, Title = "My Run", StartedAt = DateTime.UtcNow });
            _dbContext.Activities.Add(new Activity { UserId = "other-user", Title = "Other Run", StartedAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            var result = await _controller.GetHistory();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var activities = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Single(activities);
        }


        [Fact]
        public async Task GetActivityDetails_ReturnsNotFound_WhenActivityDoesNotExist()
        {
            var result = await _controller.GetActivityDetails(Guid.NewGuid());
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetActivityDetails_ReturnsNotFound_WhenActivityBelongsToAnotherUser()
        {
            var activityId = Guid.NewGuid();
            _dbContext.Activities.Add(new Activity { Id = activityId, UserId = "other-user", Title = "Secret Run" });
            await _dbContext.SaveChangesAsync();

            var result = await _controller.GetActivityDetails(activityId);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetActivityDetails_ReturnsOk_WhenActivityExistsAndBelongsToUser()
        {
            var activityId = Guid.NewGuid();
            _dbContext.Activities.Add(new Activity { Id = activityId, UserId = TestUserId, Title = "My Run" });
            await _dbContext.SaveChangesAsync();

            var result = await _controller.GetActivityDetails(activityId);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ExportGpx_ReturnsFile_WhenGeoJsonExists()
        {
            var activityId = Guid.NewGuid();
            string fakeGeoJson = "{\"type\":\"LineString\",\"coordinates\":[[19.0,50.0],[19.1,50.1]]}";

            _dbContext.Activities.Add(new Activity
            {
                Id = activityId,
                UserId = TestUserId,
                RouteGeoJson = fakeGeoJson,
                ActivityType = "Cycling",
                StartedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var result = await _controller.ExportGpx(activityId);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/gpx+xml", fileResult.ContentType);
            Assert.Contains(".gpx", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task ExportGpx_ReturnsBadRequest_WhenNoGpsData()
        {
            var activityId = Guid.NewGuid();
            _dbContext.Activities.Add(new Activity
            {
                Id = activityId,
                UserId = TestUserId,
                RouteGeoJson = null
            });
            await _dbContext.SaveChangesAsync();

            var result = await _controller.ExportGpx(activityId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Ta aktywność nie posiada zapisanej trasy GPS.", badRequest.Value);
        }

        [Fact]
        public async Task GetLeaderboard_CalculatesRankingCorrectly()
        {
            var user1 = new ApplicationUser { Id = "u1", UserName = "Runner1" };
            var user2 = new ApplicationUser { Id = "u2", UserName = "Runner2" };
            _dbContext.Users.AddRange(user1, user2);

            _dbContext.Activities.Add(new Activity { UserId = "u1", DistanceMeters = 10000, StartedAt = DateTime.UtcNow });
            _dbContext.Activities.Add(new Activity { UserId = "u1", DistanceMeters = 5000, StartedAt = DateTime.UtcNow });

            _dbContext.Activities.Add(new Activity { UserId = "u2", DistanceMeters = 20000, StartedAt = DateTime.UtcNow });

            _dbContext.Activities.Add(new Activity { UserId = "u2", DistanceMeters = 50000, StartedAt = DateTime.UtcNow.AddDays(-31) });

            await _dbContext.SaveChangesAsync();

            var result = await _controller.GetLeaderboard();

            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var ranking = root.GetProperty("Ranking");

            Assert.Equal(2, ranking.GetArrayLength()); 

            var firstPlace = ranking[0];
            Assert.Equal(1, firstPlace.GetProperty("Position").GetInt32());
            Assert.Equal("Runner2", firstPlace.GetProperty("UserName").GetString());
            Assert.Equal(20.0, firstPlace.GetProperty("TotalDistanceKm").GetDouble());


            var secondPlace = ranking[1];
            Assert.Equal(2, secondPlace.GetProperty("Position").GetInt32());
            Assert.Equal("Runner1", secondPlace.GetProperty("UserName").GetString());
            Assert.Equal(15.0, secondPlace.GetProperty("TotalDistanceKm").GetDouble());

        }



        private void SetupUserContext(string userId, string userName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
            return mgr;
        }
    }
}
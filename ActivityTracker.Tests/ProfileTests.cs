#nullable disable
using ActivityTracker.Controllers;
using ActivityTracker.Data;
using ActivityTracker.DTOs;
using ActivityTracker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ActivityTracker.Tests
{
    public class ProfileTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly ProfileController _controller;

        private const string TestUserId = "user-guid-123";

        public ProfileTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _mockUserManager = MockUserManager<ApplicationUser>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(x => x.WebRootPath).Returns(Path.GetTempPath());

            _controller = new ProfileController(
                _mockUserManager.Object,
                _mockEnvironment.Object,
                _dbContext
            );

            SetupUserContext(TestUserId, "TestUser");
        }

        [Fact]
        public async Task GetProfile_ReturnsNotFound_WhenUserDoesNotExist()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId))
                .ReturnsAsync((ApplicationUser)null);

            var result = await _controller.GetProfile();


            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_ReturnsProfileWithStats_WhenUserExists()
        {
            var user = new ApplicationUser
            {
                Id = TestUserId,
                UserName = "TestUser",
                Email = "test@test.com"
            };

            _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId)).ReturnsAsync(user);

            _dbContext.Activities.Add(new Activity { UserId = TestUserId, DistanceMeters = 1000, DurationSeconds = 100 });
            _dbContext.Activities.Add(new Activity { UserId = TestUserId, DistanceMeters = 2500, DurationSeconds = 200 });
            _dbContext.Activities.Add(new Activity { UserId = "other-user", DistanceMeters = 5000, DurationSeconds = 500 }); 
            await _dbContext.SaveChangesAsync();

            var result = await _controller.GetProfile();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<ProfileDto>(okResult.Value);

            Assert.Equal("TestUser", dto.UserName);
            Assert.Equal(2, dto.TotalActivities); 
            Assert.Equal(3.5, dto.TotalDistanceKm); 
            Assert.Equal(300, dto.TotalDurationSeconds); 
        }


        [Fact]
        public async Task UpdateProfile_ReturnsBadRequest_WhenUserNameIsTaken()
        {

            var user = new ApplicationUser { Id = TestUserId, UserName = "OldName" };
            var otherUser = new ApplicationUser { Id = "other-id", UserName = "NewName" };

            _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.FindByNameAsync("NewName")).ReturnsAsync(otherUser);

            var dto = new UpdateProfileDto { UserName = "NewName" };

            var result = await _controller.UpdateProfile(dto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Ten nick jest już zajęty.", badRequest.Value);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsOk_WhenUpdateIsSuccessful()
        {

            var user = new ApplicationUser { Id = TestUserId, UserName = "OldName", FirstName = "OldFirst" };

            _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null); 
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var dto = new UpdateProfileDto { UserName = "NewName", FirstName = "NewFirst" };
            var result = await _controller.UpdateProfile(dto);


            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("NewName", user.UserName);
            Assert.Equal("NewFirst", user.FirstName);
        }


        [Fact]
        public async Task UploadAvatar_ReturnsBadRequest_WhenFileIsNull()
        {
            var user = new ApplicationUser { Id = TestUserId };
            _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId)).ReturnsAsync(user);
            
            var result = await _controller.UploadAvatar(null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Nie przesłano pliku.", badRequest.Value);
        }

        [Fact]
        public async Task UploadAvatar_ReturnsBadRequest_WhenExtensionInvalid()
        {
            var user = new ApplicationUser { Id = TestUserId };
            _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId)).ReturnsAsync(user);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("virus.exe");

            var result = await _controller.UploadAvatar(fileMock.Object);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Tylko pliki .jpg, .jpeg, .png są dozwolone.", badRequest.Value);
        }

        [Fact]
        public async Task UploadAvatar_ReturnsOk_WhenFileIsValid()
        {
            var user = new ApplicationUser { Id = TestUserId };
            _mockUserManager.Setup(x => x.FindByIdAsync(TestUserId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);


            var fileMock = new Mock<IFormFile>();
            var content = "Fake image content";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns("avatar.png");
            fileMock.Setup(f => f.Length).Returns(ms.Length);


            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            _controller.ControllerContext.HttpContext.Request.Scheme = "http";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

            var result = await _controller.UploadAvatar(fileMock.Object);

            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            var url = doc.RootElement.GetProperty("Url").GetString();

            Assert.Contains("http://localhost/uploads/avatars/", url);
            Assert.Contains(TestUserId, url);
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

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
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
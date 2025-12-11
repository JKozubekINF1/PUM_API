#nullable disable 
using ActivityTracker.Controllers;
using ActivityTracker.DTOs;
using ActivityTracker.Models;
using ActivityTracker.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ActivityTracker.Tests
{
    public class AuthTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly JwtService _realJwtService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AuthController _controller;

        public AuthTests()
        {
            _mockUserManager = MockUserManager<ApplicationUser>();
            _mockEmailService = new Mock<IEmailService>();

            var myConfigurationSettings = new Dictionary<string, string>
            {
                {"Jwt:Key", "BardzoDlugieTajneHasloKtoreMaMinimum32Znaki123!"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpireDays", "1"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfigurationSettings)
                .Build();

            _realJwtService = new JwtService(configuration, _mockUserManager.Object);

            _controller = new AuthController(
                _mockUserManager.Object,
                _realJwtService,
                _mockEmailService.Object
            );
        }


        [Fact]
        public async Task Register_ReturnsBadRequest_WhenPasswordsDoNotMatch()
        {
            var dto = new RegisterDto { Password = "pass", ConfirmPassword = "different", UserName = "test", Email = "test@test.com" };
            var result = await _controller.Register(dto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Passwords do not match.", badRequest.Value);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUserNameTaken()
        {
            var dto = new RegisterDto { Password = "pass", ConfirmPassword = "pass", UserName = "exists", Email = "new@test.com" };
            _mockUserManager.Setup(x => x.FindByNameAsync(dto.UserName))
                .ReturnsAsync(new ApplicationUser());

            var result = await _controller.Register(dto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("This username is already taken.", badRequest.Value);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenRegistrationSuccessful()
        {
            var dto = new RegisterDto { Password = "pass", ConfirmPassword = "pass", UserName = "new", Email = "new@test.com" };

            _mockUserManager.Setup(x => x.FindByNameAsync(dto.UserName)).ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Register(dto);
            Assert.IsType<OkObjectResult>(result);
        }


        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
        {
            var dto = new LoginDto { Email = "missing@test.com", Password = "pass" };
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((ApplicationUser)null);

            var result = await _controller.Login(dto);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid credentials.", unauthorized.Value);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsValid()
        {
            var user = new ApplicationUser { Email = "valid@test.com", UserName = "validUser", MustChangePassword = false };
            var dto = new LoginDto { Email = "valid@test.com", Password = "pass" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });


            var result = await _controller.Login(dto);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseDto = Assert.IsType<LoginResponseDto>(okResult.Value);

            Assert.NotNull(responseDto.Token);
            Assert.NotEmpty(responseDto.Token);
            Assert.Equal(user.Email, responseDto.Email);
        }


        [Fact]
        public async Task ForgotPassword_ReturnsOk_EvenIfUserNotFound_ForSecurity()
        {
            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
            var dto = new ForgotPasswordDto { Email = "ghost@test.com" };

            var result = await _controller.ForgotPassword(dto);
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsBadRequest_WhenEmailServiceFails()
        {
            var user = new ApplicationUser { Email = "valid@test.com", UserName = "TestUser" };
            var dto = new ForgotPasswordDto { Email = user.Email };

            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset_token");

            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP Error"));

            var result = await _controller.ForgotPassword(dto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Wystąpił błąd podczas wysyłania e-maila", badRequest.Value.ToString());
        }


        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenPasswordsMismatch()
        {
            var dto = new ResetPasswordDto { NewPassword = "abc", ConfirmNewPassword = "xyz" };
            var result = await _controller.ResetPassword(dto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Hasła nie są identyczne.", badRequest.Value);
        }

        [Fact]
        public async Task ResetPassword_ReturnsOk_WhenSuccess()
        {
            var user = new ApplicationUser { Email = "test@test.com" };
            var dto = new ResetPasswordDto { Email = user.Email, NewPassword = "new", ConfirmNewPassword = "new", Token = "token" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ResetPassword(dto);
            Assert.IsType<OkObjectResult>(result);
        }


        [Fact]
        public async Task ChangeInitialPassword_ReturnsOk_WhenSuccess()
        {
            var userId = "user123";
            var user = new ApplicationUser { Id = userId, MustChangePassword = true };
            var dto = new ChangePasswordDto { NewPassword = "newPass" };

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext { User = claimsPrincipal };
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, "token", dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ChangeInitialPassword(dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.False(user.MustChangePassword);
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
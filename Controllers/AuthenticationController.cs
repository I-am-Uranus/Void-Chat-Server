using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Void.Models.Auth;
using AuthenticationService = Void.Services.AuthenticationService;

namespace Void.Controllers
{
    [Route("api/authentication")]
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthenticationService _authenticationService;

        public AuthenticationController(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            try
            {
                _authenticationService.Register(
                    request.Username,
                    request.DisplayName,
                    request.Password,
                    request.ConfirmPassword,
                    request.Email,
                    request.ProfilePicture
                );

                return Ok(new { message = "User registered successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
        {
            var user = _authenticationService.SignIn(request.Username, request.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            await SignInUserAsync(user.Id, user.UserName ?? string.Empty);

            return Ok(new
            {
                id = user.Id,
                username = user.UserName,
                displayName = user.DisplayName,
                profilePicture = user.ProfilePicture,
                message = "Signed in successfully"
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out" });
        }

        [HttpGet("me")]
        [HttpGet("check")]
        public IActionResult Me()
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized(new { message = "Not authenticated" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Invalid session" });

            var user = _authenticationService.GetUserById(userId);
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            return Ok(new
            {
                id = user.Id,
                username = user.UserName,
                displayName = user.DisplayName,
                email = user.Email,
                profilePicture = user.ProfilePicture,
                message = $"Hello {user.DisplayName ?? user.UserName}"
            });
        }

        private async Task SignInUserAsync(int userId, string username)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, username)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Void.DTOs;
using Void.Services;

namespace Void.Controllers
{
    [Route("api/settings")]
    [ApiController]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly SettingsService _settingsService;

        public SettingsController(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpPatch("profile")]
        public IActionResult UpdateProfile([FromBody] UpdateProfileDTO? request)
        {
            if (request == null)
                return BadRequest(new { error = "Invalid settings data" });

            try
            {
                var userId = GetCurrentUserId();
                var user = _settingsService.UpdateProfile(userId, request.DisplayName, request.ProfilePicture);

                return Ok(new
                {
                    message = "Profile updated",
                    user
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPatch("display-name")]
        public IActionResult UpdateDisplayName([FromBody] UpdateDisplayNameDTO? request)
        {
            if (request == null)
                return BadRequest(new { error = "Invalid settings data" });

            try
            {
                var userId = GetCurrentUserId();
                var user = _settingsService.UpdateDisplayName(userId, request.DisplayName);

                return Ok(new
                {
                    message = "Display name updated",
                    user
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPatch("profile-picture")]
        public IActionResult UpdateProfilePicture([FromBody] UpdateProfilePictureDTO? request)
        {
            if (request == null)
                return BadRequest(new { error = "Invalid settings data" });

            try
            {
                var userId = GetCurrentUserId();
                var user = _settingsService.UpdateProfilePicture(userId, request.ProfilePicture);

                return Ok(new
                {
                    message = "Profile picture updated",
                    user
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("User not authenticated");

            return userId;
        }
    }
}

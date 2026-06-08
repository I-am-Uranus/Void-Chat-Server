using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Void.Services;

namespace Void.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationsController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] string? type = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            try
            {
                var notifications = await _notificationService.GetForUserAsync(userId.Value, type);
                return Ok(notifications);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] string? type = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            try
            {
                var count = await _notificationService.GetUnreadCountAsync(userId.Value, type);
                return Ok(new { count });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var updated = await _notificationService.MarkAsReadAsync(id, userId.Value);
            if (!updated)
            {
                return NotFound(new { error = "Notification not found" });
            }

            return Ok(new { message = "Notification marked as read" });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] string? type = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            try
            {
                var updatedCount = await _notificationService.MarkAllAsReadAsync(userId.Value, type);
                return Ok(new { message = "Notifications marked as read", count = updatedCount });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var deleted = await _notificationService.DeleteAsync(id, userId.Value);
            if (!deleted)
            {
                return NotFound(new { error = "Notification not found" });
            }

            return Ok(new { message = "Notification deleted" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAll([FromQuery] string? type = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            try
            {
                var deletedCount = await _notificationService.DeleteAllAsync(userId.Value, type);
                return Ok(new { message = "Notifications deleted", count = deletedCount });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}

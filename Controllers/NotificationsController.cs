using Microsoft.AspNetCore.Mvc;
using Void.Services;

namespace Void.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationsController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("sendToUser")]
        public async Task<IActionResult> SendToUser([FromBody] SendNotificationRequest req)
        {
            await _notificationService.SendToUser(req.UserId, new { req.Title, req.Message });
            return Ok();
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastNotificationRequest req)
        {
            await _notificationService.Broadcast(new { req.Title, req.Message });
            return Ok();
        }
    }

    public class SendNotificationRequest
    {
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class BroadcastNotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

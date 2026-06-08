using Microsoft.AspNetCore.SignalR;
using Void.Hubs;

namespace Void.Services
{
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task SendToUser(int userId, object notification)
        {
            return _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
        }

        public Task Broadcast(object notification)
        {
            return _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        }
    }
}

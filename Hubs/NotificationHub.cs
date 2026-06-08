using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;

namespace Void.Hubs
{
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<int, HashSet<string>> ConnectedUsers = new();

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromClaims();
            if (userId != null)
            {
                ConnectedUsers.AddOrUpdate(
                    userId.Value,
                    _ => new HashSet<string> { Context.ConnectionId },
                    (_, set) => { set.Add(Context.ConnectionId); return set; });

                Debug.WriteLine($"Notification client connected: {Context.ConnectionId} (UserId: {userId})");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromClaims();
            if (userId != null && ConnectedUsers.TryGetValue(userId.Value, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                    ConnectedUsers.TryRemove(userId.Value, out _);

                Debug.WriteLine($"Notification client disconnected: {Context.ConnectionId} (UserId: {userId})");
            }

            await base.OnDisconnectedAsync(exception);
        }

        private int? GetUserIdFromClaims()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                return userId;
            return null;
        }
    }
}

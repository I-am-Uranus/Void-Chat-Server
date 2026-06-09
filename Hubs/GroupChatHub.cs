using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using Void.DTOs;
using Void.Services;

namespace Void.Hubs
{
    [Authorize]
    public class GroupChatHub : Hub
    {
        private readonly GroupService _groupService;
        private static readonly ConcurrentDictionary<int, HashSet<string>> ConnectedUsers = new();

        public GroupChatHub(GroupService groupService)
        {
            _groupService = groupService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromClaims();
            if (userId.HasValue)
            {
                ConnectedUsers.AddOrUpdate(
                    userId.Value,
                    _ => new HashSet<string> { Context.ConnectionId },
                    (_, set) => { set.Add(Context.ConnectionId); return set; });

                Debug.WriteLine($"GroupChatHub connected: {Context.ConnectionId} (UserId: {userId})");
                await Clients.All.SendAsync("OnlineUsersCount", ConnectedUsers.Count);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromClaims();
            if (userId.HasValue && ConnectedUsers.TryGetValue(userId.Value, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                    ConnectedUsers.TryRemove(userId.Value, out _);

                Debug.WriteLine($"GroupChatHub disconnected: {Context.ConnectionId} (UserId: {userId})");
                await Clients.All.SendAsync("OnlineUsersCount", ConnectedUsers.Count);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<GroupMessageDTO> SendMessageToGroup(int groupId, string message)
        {
            var senderId = GetUserIdFromClaims();
            if (!senderId.HasValue)
                throw new HubException("User not authenticated.");

            if (groupId <= 0)
                throw new HubException("Invalid group.");

            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message cannot be empty.");

            try
            {
                return await _groupService.SendMessageAsync(groupId, senderId.Value, message);
            }
            catch (ArgumentException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SendMessageToGroup error: {ex.Message}");
                throw new HubException("Failed to send message.");
            }
        }

        public async Task<GroupMessageDTO> SendImageToGroup(int groupId, string? imageData, string? imageMimeType)
        {
            var senderId = GetUserIdFromClaims();
            if (!senderId.HasValue)
                throw new HubException("User not authenticated.");

            if (groupId <= 0)
                throw new HubException("Invalid group.");

            if (string.IsNullOrWhiteSpace(imageData))
                throw new HubException("Image data is required.");

            try
            {
                return await _groupService.SendMessageAsync(groupId, senderId.Value, string.Empty, imageData, imageMimeType);
            }
            catch (ArgumentException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SendImageToGroup error: {ex.Message}");
                throw new HubException("Failed to send image.");
            }
        }

        private int? GetUserIdFromClaims()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}

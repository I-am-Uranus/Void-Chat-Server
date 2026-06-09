using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using Void.DTOs;
using Void.Services;

namespace Void.Hubs
{
    public class PrivateChatHub : Hub
    {
        private readonly ChatService _chatService;

        private static readonly ConcurrentDictionary<int, HashSet<string>> ConnectedUsers = new();

        public PrivateChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromClaims();
            if (userId != null)
            {
                ConnectedUsers.AddOrUpdate(
                    userId.Value,
                    _ => new HashSet<string> { Context.ConnectionId },
                    (_, set) => { set.Add(Context.ConnectionId); return set; });

                Debug.WriteLine($"Client connected: {Context.ConnectionId} (UserId: {userId})");
                await Clients.All.SendAsync("OnlineUsersCount", ConnectedUsers.Count);
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

                Debug.WriteLine($"Client disconnected: {Context.ConnectionId} (UserId: {userId})");
                await Clients.All.SendAsync("OnlineUsersCount", ConnectedUsers.Count);
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
        public Task<bool> IsUserOnline(int userId)
        {
            return Task.FromResult(ConnectedUsers.ContainsKey(userId));
        }


        public async Task SendPrivateMessage(PrivateMessageCreateDTO messageDto)
        {
            var senderId = GetUserIdFromClaims();
            if (!senderId.HasValue)
            {
                throw new HubException("User not authenticated.");
            }

            if (messageDto.ReceiverId <= 0)
            {
                throw new HubException("Receiver is required.");
            }

            if (string.IsNullOrWhiteSpace(messageDto.Content) &&
                string.IsNullOrWhiteSpace(messageDto.ImageData))
            {
                throw new HubException("Message cannot be empty.");
            }

            await _chatService.SendMessageAsync(new ChatCreateDTO
            {
                SenderId = senderId.Value,
                ReceiverId = messageDto.ReceiverId,
                Content = messageDto.Content,
                ImageData = messageDto.ImageData,
                ImageMimeType = messageDto.ImageMimeType
            });
        }
    }
}

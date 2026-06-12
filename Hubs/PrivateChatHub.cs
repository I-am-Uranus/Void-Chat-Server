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
            if (userId.HasValue)
            {
                ConnectedUsers.AddOrUpdate(
                    userId.Value,
                    _ => new HashSet<string> { Context.ConnectionId },
                    (_, set) => { set.Add(Context.ConnectionId); return set; });

                Debug.WriteLine($"PrivateChatHub connected: {Context.ConnectionId} (UserId: {userId})");
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

                Debug.WriteLine($"PrivateChatHub disconnected: {Context.ConnectionId} (UserId: {userId})");
                await Clients.All.SendAsync("OnlineUsersCount", ConnectedUsers.Count);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<ChatWithUserDTO> SendPrivateMessage(PrivateMessageCreateDTO messageDto)
        {
            var senderId = GetUserIdFromClaims();
            if (!senderId.HasValue)
                throw new HubException("User not authenticated.");

            if (messageDto.ReceiverId <= 0)
                throw new HubException("Receiver is required.");

            if (string.IsNullOrWhiteSpace(messageDto.Content) && string.IsNullOrWhiteSpace(messageDto.ImageData))
                throw new HubException("Message cannot be empty.");

            try
            {
                return await _chatService.SendMessageAsync(new ChatCreateDTO
                {
                    SenderId = senderId.Value,
                    ReceiverId = messageDto.ReceiverId,
                    Content = messageDto.Content,
                    ImageData = messageDto.ImageData,
                    ImageMimeType = messageDto.ImageMimeType
                });
            }
            catch (ArgumentException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SendPrivateMessage error for sender {senderId}: {ex.Message}");
                throw new HubException("Failed to send message.");
            }
        }

        public Task<bool> IsUserOnline(int userId)
        {
            return Task.FromResult(ConnectedUsers.ContainsKey(userId));
        }

        private int? GetUserIdFromClaims()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}

using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Collections.Generic;
using Void.Database;
using Void.Models;

namespace Void.Hubs
{
    public class GroupChatHub : Hub
    {
        private readonly DatabaseContext _context;
        private readonly Void.Services.NotificationService _notificationService;
        private static readonly HashSet<int> ConnectedUsers = new();

        public GroupChatHub(DatabaseContext context, Void.Services.NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext();
            if (userId.HasValue) ConnectedUsers.Add(userId.Value);
            await Clients.All.SendAsync("OnlineUsersCount", ConnectedUsers.Count);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromContext();
            if (userId.HasValue) ConnectedUsers.Remove(userId.Value);
            await Clients.All.SendAsync("OnlineUsersCount", ConnectedUsers.Count);
            await base.OnDisconnectedAsync(exception);
        }

        private int? GetUserIdFromContext()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null && httpContext.Request.Query.TryGetValue("userId", out var userIdStr))
                if (int.TryParse(userIdStr, out var userId)) return userId;
            return null;
        }

        public async Task SendMessageToGroup(int groupId, string message)
        {
            var senderId = GetUserIdFromContext();
            if (!senderId.HasValue) return;

            var groupMessage = new GroupMessage
            {
                GroupId = groupId,
                SenderId = senderId.Value,
                Content = message,
                Timestamp = DateTime.UtcNow
            };

            _context.GroupMessages.Add(groupMessage);
            await _context.SaveChangesAsync();

            // notify group via hub
            await Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupMessage", groupMessage);

            // send lightweight notifications to group members (except sender)
            var memberIds = _context.GroupMembers.Where(m => m.GroupId == groupId && m.UserId != senderId.Value).Select(m => m.UserId).ToList();
            foreach (var memberId in memberIds)
            {
                await _notificationService.SendToUser(memberId, new { Type = "NewGroupMessage", GroupId = groupId, From = senderId.Value, MessageId = groupMessage.Id, Preview = (groupMessage.Content ?? string.Empty).Substring(0, Math.Min(200, (groupMessage.Content ?? string.Empty).Length)) });
            }
        }
        public async Task SendMessage(string message)
        {
            var senderId = GetUserIdFromContext();
            if (!senderId.HasValue) return;

            await Clients.All.SendAsync("ReceiveMessage", new
            {
                SenderId = senderId.Value,
                Content = message
            });
        }

    }
}
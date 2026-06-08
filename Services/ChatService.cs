using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Collections.Generic;
using Void.DTOs;
using Void.Hubs;
using Void.Models;
using Void.Repositories;

namespace Void.Services
{
    public class ChatService
    {
        private readonly ChatRepository _repository;
        private readonly IHubContext<PrivateChatHub> _hub;
        private readonly NotificationService _notificationService;

        public ChatService(ChatRepository repository, IHubContext<PrivateChatHub> hub, NotificationService notificationService)
        {
            _repository = repository;
            _hub = hub;
            _notificationService = notificationService;
        }

        public async Task<List<ChatWithUserDTO>> GetConversationAsync(int user1, int user2)
        {
            var chats = await _repository.GetConversationAsync(user1, user2);

            var unread = chats.Where(c => c.ReceiverId == user1 && !c.IsRead.GetValueOrDefault()).ToList();
            foreach (var chat in unread) chat.IsRead = true;
            await Task.WhenAll(unread.Select(c => _repository.UpdateAsync(c)));

            return chats.Select(c => new ChatWithUserDTO
            {
                Id = c.Id,
                Content = c.Content ?? "",
                Timestamp = c.Timestamp ?? DateTime.UtcNow,
                SenderId = c.SenderId ?? 0,
                SenderName = c.Sender?.UserName ?? "Unknown",
                ReceiverId = c.ReceiverId ?? 0,
                ReceiverName = c.Receiver?.UserName ?? "Unknown",
                IsRead = c.IsRead ?? false
            }).ToList();
        }

        public async Task<ChatWithUserDTO> SendMessageAsync(ChatCreateDTO chatDto)
        {
            var chat = new Chat
            {
                SenderId = chatDto.SenderId,
                ReceiverId = chatDto.ReceiverId,
                Content = chatDto.Content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            await _repository.AddAsync(chat);

            var result = new ChatWithUserDTO
            {
                Id = chat.Id,
                Content = chat.Content,
                Timestamp = chat.Timestamp ?? DateTime.UtcNow,
                SenderId = chat.SenderId ?? 0,
                SenderName = chat.Sender?.UserName ?? "Unknown",
                ReceiverId = chat.ReceiverId ?? 0,
                ReceiverName = chat.Receiver?.UserName ?? "Unknown",
                IsRead = chat.IsRead ?? false
            };

            await _hub.Clients.User(chat.ReceiverId.ToString()).SendAsync("ReceiveMessage", result);

            // send a lightweight notification for new private message
            if (chat.ReceiverId.HasValue)
            {
                await _notificationService.SendToUser(chat.ReceiverId.Value, new { Type = "NewPrivateMessage", From = chat.SenderId, MessageId = chat.Id, Preview = (chat.Content ?? string.Empty).Substring(0, Math.Min(200, (chat.Content ?? string.Empty).Length)) });
            }

            return result;
        }

        public async Task<int> MarkConversationAsSeen(int viewerId, int otherUserId)
        {
            var chats = await _repository.GetConversationAsync(viewerId, otherUserId);

            var unread = chats.Where(c => c.ReceiverId == viewerId && !c.IsRead.GetValueOrDefault()).ToList();
            foreach (var chat in unread)
            {
                chat.IsRead = true;
                await _repository.UpdateAsync(chat);
                // notify sender that this message was seen
                if (chat.SenderId.HasValue)
                    await _hub.Clients.User(chat.SenderId.Value.ToString()).SendAsync("MessageSeen", new { MessageId = chat.Id, SeenBy = viewerId });
            }

            if (unread.Count > 0)
            {
                // notify the other user that their messages were seen by viewer
                await _hub.Clients.User(otherUserId.ToString()).SendAsync("MessagesSeen", new { ViewerId = viewerId, Count = unread.Count });
            }

            return unread.Count;
        }

        public async Task<bool> MarkMessageAsSeen(int messageId, int viewerId)
        {
            var chat = await _repository.GetByIdAsync(messageId);
            if (chat == null) return false;

            // only the receiver can mark as seen
            if (chat.ReceiverId != viewerId) return false;

            if (chat.IsRead.GetValueOrDefault()) return true;

            chat.IsRead = true;
            await _repository.UpdateAsync(chat);

            if (chat.SenderId.HasValue)
            {
                await _hub.Clients.User(chat.SenderId.Value.ToString()).SendAsync("MessageSeen", new { MessageId = chat.Id, SeenBy = viewerId });
            }

            return true;
        }
    }
}

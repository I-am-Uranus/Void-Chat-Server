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

            var unread = chats
                .Where(c => c.ReceiverId == user1 && !c.IsRead.GetValueOrDefault())
                .ToList();

            foreach (var chat in unread)
            {
                chat.IsRead = true;
                await _repository.UpdateAsync(chat);
            }
            return chats.Select(ToChatWithUserDTO).ToList();
        }

        public async Task<ChatWithUserDTO> SendMessageAsync(ChatCreateDTO chatDto)
        {
            var chat = new Chat
            {
                SenderId = chatDto.SenderId,
                ReceiverId = chatDto.ReceiverId,
                Content = chatDto.Content,
                ImageData = chatDto.ImageData,
                ImageMimeType = chatDto.ImageMimeType,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            await _repository.AddAsync(chat);

            var savedChat = chat;
            if (chat.SenderId.HasValue && chat.ReceiverId.HasValue)
            {
                var conversation = await _repository.GetConversationAsync(chat.SenderId.Value, chat.ReceiverId.Value);
                savedChat = conversation.FirstOrDefault(c => c.Id == chat.Id) ?? chat;
            }

            var result = ToChatWithUserDTO(savedChat);

            if (savedChat.SenderId.HasValue)
            {
                await _hub.Clients.User(savedChat.SenderId.Value.ToString())
                    .SendAsync("ReceiveMessage", result);
            }

            if (savedChat.ReceiverId.HasValue)
            {
                await _hub.Clients.User(savedChat.ReceiverId.Value.ToString())
                    .SendAsync("ReceiveMessage", result);

                var previewMessage = BuildPreviewMessage(savedChat);
                await _notificationService.CreateAsync(
                    recipientUserId: savedChat.ReceiverId.Value,
                    senderUserId: savedChat.SenderId,
                    type: NotificationTypes.Chat,
                    title: "New message",
                    message: previewMessage,
                    relatedEntityId: savedChat.Id
                );
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

        private static ChatWithUserDTO ToChatWithUserDTO(Chat chat)
        {
            return new ChatWithUserDTO
            {
                Id = chat.Id,
                Content = chat.Content ?? "",
                Timestamp = chat.Timestamp ?? DateTime.UtcNow,
                SenderId = chat.SenderId ?? 0,
                SenderName = chat.Sender?.UserName ?? "Unknown",
                SenderLastActive = chat.Sender?.LastActive,
                ReceiverId = chat.ReceiverId ?? 0,
                ReceiverName = chat.Receiver?.UserName ?? "Unknown",
                ReceiverLastActive = chat.Receiver?.LastActive,
                ImageData = chat.ImageData,
                ImageMimeType = chat.ImageMimeType,
                IsRead = chat.IsRead ?? false
            };
        }

        private static string BuildPreviewMessage(Chat chat)
        {
            var content = chat.Content ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(content))
            {
                return content[..Math.Min(200, content.Length)];
            }

            return string.IsNullOrWhiteSpace(chat.ImageData)
                ? string.Empty
                : "Sent you an image.";
        }
    }
}

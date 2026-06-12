using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
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

            if (unread.Count > 0)
            {
                foreach (var chat in unread)
                    chat.IsRead = true;

                await _repository.SaveChangesAsync();
            }

            return chats.Select(ToChatWithUserDTO).ToList();
        }

        public async Task<ChatWithUserDTO> SendMessageAsync(ChatCreateDTO chatDto)
        {
            ValidateMessage(chatDto.Content, chatDto.ImageData, chatDto.ImageMimeType);

            var chat = new Chat
            {
                SenderId = chatDto.SenderId,
                ReceiverId = chatDto.ReceiverId,
                Content = chatDto.Content ?? string.Empty,
                ImageData = Normalize(chatDto.ImageData),
                ImageMimeType = Normalize(chatDto.ImageMimeType),
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            await _repository.AddAsync(chat);

            // Reload the saved message with navigation properties (Sender, Receiver)
            var savedChat = await _repository.GetByIdAsync(chat.Id) ?? chat;

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

                try
                {
                    var preview = BuildPreviewMessage(savedChat);
                    await _notificationService.CreateAsync(
                        recipientUserId: savedChat.ReceiverId.Value,
                        senderUserId: savedChat.SenderId,
                        type: NotificationTypes.Chat,
                        title: "New message",
                        message: preview,
                        relatedEntityId: savedChat.Id);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ChatService notification error: {ex.Message}");
                }
            }

            return result;
        }

        private static void ValidateMessage(string? content, string? imageData, string? imageMimeType)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(imageData))
                errors.Add("Message content or image is required.");

            MessageImageValidator.Validate(imageData, imageMimeType, errors);

            if (errors.Any())
                throw new ArgumentException(string.Join(Environment.NewLine, errors));
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public async Task<int> MarkConversationAsSeen(int viewerId, int otherUserId)
        {
            var chats = await _repository.GetConversationAsync(viewerId, otherUserId);
            var unread = chats
                .Where(c => c.ReceiverId == viewerId && !c.IsRead.GetValueOrDefault())
                .ToList();

            if (unread.Count == 0)
                return 0;

            foreach (var chat in unread)
                chat.IsRead = true;

            await _repository.SaveChangesAsync();

            foreach (var chat in unread)
            {
                if (chat.SenderId.HasValue)
                {
                    await _hub.Clients.User(chat.SenderId.Value.ToString())
                        .SendAsync("MessageSeen", new { MessageId = chat.Id, SeenBy = viewerId });
                }
            }

            await _hub.Clients.User(otherUserId.ToString())
                .SendAsync("MessagesSeen", new { ViewerId = viewerId, Count = unread.Count });

            return unread.Count;
        }

        public async Task<bool> MarkMessageAsSeen(int messageId, int viewerId)
        {
            var chat = await _repository.GetByIdAsync(messageId);
            if (chat == null) return false;

            if (chat.ReceiverId != viewerId) return false;

            if (chat.IsRead.GetValueOrDefault()) return true;

            chat.IsRead = true;
            await _repository.UpdateAsync(chat);

            if (chat.SenderId.HasValue)
            {
                await _hub.Clients.User(chat.SenderId.Value.ToString())
                    .SendAsync("MessageSeen", new { MessageId = chat.Id, SeenBy = viewerId });
            }

            return true;
        }

        private static ChatWithUserDTO ToChatWithUserDTO(Chat chat)
        {
            return new ChatWithUserDTO
            {
                Id = chat.Id,
                Content = chat.Content ?? string.Empty,
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
                return content[..Math.Min(200, content.Length)];

            return string.IsNullOrWhiteSpace(chat.ImageData) ? string.Empty : "Sent you an image.";
        }
    }
}

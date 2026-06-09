using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using Void.DTOs;
using Void.Hubs;
using Void.Models;
using Void.Repositories;

namespace Void.Services
{
    public class GroupService
    {
        private readonly GroupRepository _groupRepository;
        private readonly IHubContext<GroupChatHub> _hubContext;
        private readonly NotificationService _notificationService;

        public GroupService(
            GroupRepository groupRepository,
            IHubContext<GroupChatHub> hubContext,
            NotificationService notificationService)
        {
            _groupRepository = groupRepository;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        public async Task<Group> CreateGroupAsync(string name, List<int> memberIds)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Group name is required.");

            return await _groupRepository.CreateGroupAsync(name, memberIds);
        }

        public async Task<Group?> GetGroupAsync(int groupId)
        {
            return await _groupRepository.GetGroupAsync(groupId);
        }

        public async Task<List<Group>> GetGroupsForUserAsync(int userId)
        {
            return await _groupRepository.GetGroupsForUserAsync(userId);
        }

        public async Task AddMemberAsync(int groupId, int userId)
        {
            await _groupRepository.AddMemberAsync(groupId, userId);
            await _notificationService.SendToUser(userId, new { Type = "AddedToGroup", GroupId = groupId });
        }

        public async Task RemoveMemberAsync(int groupId, int userId)
        {
            await _groupRepository.RemoveMemberAsync(groupId, userId);
        }

        public async Task<List<GroupMessageDTO>> GetMessagesAsync(int groupId)
        {
            var messages = await _groupRepository.GetMessagesAsync(groupId);
            return messages.Select(ToDTO).ToList();
        }

        public async Task<GroupMessageDTO> SendMessageAsync(
            int groupId,
            int senderId,
            string content,
            string? imageData = null,
            string? imageMimeType = null)
        {
            var group = await _groupRepository.GetGroupAsync(groupId);
            if (group == null)
                throw new ArgumentException("Group not found.");

            var isMember = group.Members.Any(m => m.UserId == senderId);
            if (!isMember)
                throw new UnauthorizedAccessException("You are not a member of this group.");

            var message = await _groupRepository.AddMessageAsync(groupId, senderId, content, imageData, imageMimeType);
            var dto = ToDTO(message);

            var memberIds = group.Members.Select(m => m.UserId.ToString()).ToList();
            await _hubContext.Clients.Users(memberIds).SendAsync("ReceiveGroupMessage", dto);

            foreach (var member in group.Members.Where(m => m.UserId != senderId))
            {
                try
                {
                    await _notificationService.CreateAsync(
                        recipientUserId: member.UserId,
                        senderUserId: senderId,
                        type: NotificationTypes.Group,
                        title: $"New message in {group.Name}",
                        message: BuildPreview(content),
                        relatedEntityId: message.Id);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GroupService notification error for member {member.UserId}: {ex.Message}");
                }
            }

            return dto;
        }

        private static GroupMessageDTO ToDTO(GroupMessage message) => new GroupMessageDTO
        {
            Id = message.Id,
            Content = message.Content ?? string.Empty,
            Timestamp = message.Timestamp,
            SenderId = message.SenderId,
            SenderName = message.Sender?.UserName ?? "Unknown",
            SenderProfilePicture = message.Sender?.ProfilePicture,
            GroupId = message.GroupId,
            ImageData = message.ImageData,
            ImageMimeType = message.ImageMimeType
        };

        private static string BuildPreview(string? content) =>
            string.IsNullOrWhiteSpace(content)
                ? "Sent an image."
                : content[..Math.Min(200, content.Length)];
    }
}

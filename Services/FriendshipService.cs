using Void.DTOs;
using Void.Models;
using Void.Repositories;

namespace Void.Services
{
    public class FriendshipService
    {
        private readonly FriendshipRepository _friendshipRepository;
        private readonly FriendRequestRepository _friendRequestRepository;
        private readonly BlockRepository _blockRepository;
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;

        public FriendshipService(
            FriendshipRepository friendshipRepository,
            FriendRequestRepository friendRequestRepository,
            BlockRepository blockRepository,
            UserService userService,
            NotificationService notificationService)
        {
            _friendshipRepository = friendshipRepository;
            _friendRequestRepository = friendRequestRepository;
            _blockRepository = blockRepository;
            _userService = userService;
            _notificationService = notificationService;
        }

        public async Task<FriendRequest> SendFriendRequest(int requesterId, int recipientId)
        {
            if (requesterId == recipientId)
                throw new ArgumentException("Cannot send friend request to yourself");

            var recipient = await _userService.GetByIdAsync(recipientId);
            if (recipient == null)
                throw new ArgumentException("User not found");

            var requester = await _userService.GetByIdAsync(requesterId);
            var requesterName = requester?.UserName ?? "Someone";

            if (await _blockRepository.IsBlockedBetweenUsersAsync(requesterId, recipientId))
                throw new InvalidOperationException("Cannot send friend request because one of the users has blocked the other");

            if (await _friendshipRepository.AreFriends(requesterId, recipientId))
                throw new InvalidOperationException("Already friends");

            var existingRequest = await _friendRequestRepository.GetPendingBetweenUsersAsync(requesterId, recipientId);
            if (existingRequest != null)
                throw new InvalidOperationException("Friend request already pending");

            var request = new FriendRequest
            {
                RequesterId = requesterId,
                RecipientId = recipientId,
                Status = FriendshipStatus.Pending
            };

            await _friendRequestRepository.AddAsync(request);

            await _notificationService.CreateAsync(
                recipientId,
                requesterId,
                NotificationTypes.FriendRequest,
                "New friend request",
                $"{requesterName} sent you a friend request.",
                request.Id);

            return request;
        }

        public async Task<FriendRequest> RespondToFriendRequest(int requestId, int userId, bool accept)
        {
            var request = await _friendRequestRepository.GetByIdAsync(requestId);
            if (request == null || request.RecipientId != userId)
                throw new ArgumentException("Friend request not found");

            if (request.Status != FriendshipStatus.Pending)
                throw new InvalidOperationException("Friend request already processed");

            if (accept && await _blockRepository.IsBlockedBetweenUsersAsync(request.RequesterId, request.RecipientId))
                throw new InvalidOperationException("Cannot accept friend request because one of the users has blocked the other");

            request.Status = accept ? FriendshipStatus.Accepted : FriendshipStatus.Rejected;
            request.UpdatedAt = DateTime.UtcNow;

            if (accept)
            {
                var existingFriendship = await _friendshipRepository.GetByUsersAsync(request.RequesterId, request.RecipientId);
                if (existingFriendship == null)
                {
                    var userAId = Math.Min(request.RequesterId, request.RecipientId);
                    var userBId = Math.Max(request.RequesterId, request.RecipientId);

                    await _friendshipRepository.AddAsync(new Friendship
                    {
                        UserAId = userAId,
                        UserBId = userBId
                    });
                }
            }

            await _friendRequestRepository.UpdateAsync(request);
            await _notificationService.MarkRelatedAsReadAsync(userId, NotificationTypes.FriendRequest, request.Id);
            return request;
        }

       public async Task<List<FriendshipDTO>> GetFriends(int userId)
{
    var friendships = await _friendshipRepository.GetFriendsForUserAsync(userId);

    return friendships.Select(f =>
    {
        var friend = f.UserAId == userId ? f.UserB : f.UserA;

        return new FriendshipDTO
        {
            Id = f.Id,
            UserId = userId,
            FriendId = friend.Id,
            FriendUsername = friend.UserName ?? string.Empty,
            FriendDisplayName = friend.DisplayName ?? friend.UserName ?? "Unknown user",
            FriendProfilePicture = friend.ProfilePicture,
            Status = FriendshipStatus.Accepted,
            CreatedAt = f.CreatedAt
        };
    }).ToList();
}

        public async Task<List<FriendshipDTO>> GetPendingRequests(int userId)
        {
            var requests = await _friendRequestRepository.GetPendingRequestsForRecipientAsync(userId);

            return requests.Select(r => new FriendshipDTO
            {
                Id = r.Id,
                UserId = r.RequesterId,
                FriendId = r.RecipientId,
                FriendUsername = r.Requester.UserName ?? string.Empty,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<bool> RemoveFriend(int userId, int friendId)
        {
            var friendship = await _friendshipRepository.GetByUsersAsync(userId, friendId);
            if (friendship == null)
                return false;

            return await _friendshipRepository.DeleteAsync(friendship.Id);
        }

        public async Task<bool> BlockUser(int userId, int blockUserId)
        {
            if (userId == blockUserId)
                throw new ArgumentException("Cannot block yourself");

            if (await _blockRepository.IsBlockedBetweenUsersAsync(userId, blockUserId))
                return true;

            var friendship = await _friendshipRepository.GetByUsersAsync(userId, blockUserId);
            if (friendship != null)
            {
                await _friendshipRepository.DeleteAsync(friendship.Id);
            }

            var existingRequest = await _friendRequestRepository.GetByUsersAsync(userId, blockUserId);
            if (existingRequest != null)
            {
                await _friendRequestRepository.DeleteAsync(existingRequest.Id);
            }

            await _blockRepository.AddAsync(new UserBlock
            {
                BlockerId = userId,
                BlockedId = blockUserId
            });

            return true;
        }

        public async Task<bool> AreFriends(int userId, int friendId)
        {
            return await _friendshipRepository.AreFriends(userId, friendId);
        }

        public async Task<bool> UnblockUser(int userId, int unblockUserId)
        {
            if (userId == unblockUserId)
                throw new ArgumentException("Cannot unblock yourself");

            var block = await _blockRepository.GetByUsersAsync(userId, unblockUserId);
            if (block == null)
                return false;

            if (block.BlockerId != userId)
                throw new UnauthorizedAccessException("You can only unblock users you have blocked");

            return await _blockRepository.DeleteAsync(block.Id);
        }
    }
}

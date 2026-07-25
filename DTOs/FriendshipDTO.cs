using VoidPart2.Models;

namespace VoidPart2.DTOs
{
    public class FriendshipDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int FriendId { get; set; }
        public string FriendUsername { get; set; } = string.Empty;
        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FriendDisplayName { get; set; } = string.Empty;
        public string? FriendProfilePicture { get; set; }
        public DateTime? FriendLastActive { get; set; }
    }

    public class FriendshipRequestDTO
    {
        public int FriendId { get; set; }
    }

    public class FriendshipResponseDTO
    {
        public int RequestId { get; set; }
        public int FriendshipId { get; set; }
        public bool Accept { get; set; }
    }
}

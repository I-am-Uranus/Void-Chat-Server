namespace VoidPart2.Models
{
    public static class NotificationTypes
    {
        public const string All = "All";
        public const string Group = "Group";
        public const string Chat = "Chat";
        public const string FriendRequest = "FriendRequest";

        private static readonly string[] ValidTypes = [All, Group, Chat, FriendRequest];
        private static readonly string[] FriendRequestAliases =
        [
            FriendRequest,
            "friendRequest",
            "friend_request",
            "Friend Request",
            "friend request",
            "friend-request",
            "Friend-Request"
        ];

        public static string Normalize(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return All;
            }

            var normalizedInput = NormalizeKey(type);

            foreach (var validType in ValidTypes)
            {
                if (NormalizeKey(validType) == normalizedInput)
                {
                    return validType;
                }
            }

            throw new ArgumentException("Invalid notification type. Valid types are All, Group, Chat, FriendRequest.");
        }

        public static IReadOnlyCollection<string> GetStorageAliases(string type)
        {
            var normalizedType = Normalize(type);

            return normalizedType == FriendRequest
                ? FriendRequestAliases
                : [normalizedType];
        }

        private static string NormalizeKey(string type)
        {
            return type
                .Trim()
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
        }
    }
}

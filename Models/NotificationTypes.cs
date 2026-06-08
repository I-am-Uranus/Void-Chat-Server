namespace Void.Models
{
    public static class NotificationTypes
    {
        public const string All = "All";
        public const string Group = "Group";
        public const string Chat = "Chat";
        public const string FriendRequest = "FriendRequest";

        private static readonly string[] ValidTypes = [All, Group, Chat, FriendRequest];

        public static string Normalize(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return All;
            }

            foreach (var validType in ValidTypes)
            {
                if (string.Equals(validType, type.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return validType;
                }
            }

            throw new ArgumentException("Invalid notification type. Valid types are All, Group, Chat, FriendRequest.");
        }
    }
}

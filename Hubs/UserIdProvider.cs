using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace VoidPart2.Hubs
{
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}

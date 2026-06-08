using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Void.Database;
using Void.Models;
using Void.Services;

[ApiController]
[Route("api/groups")]
public class GroupController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly NotificationService _notificationService;

    public GroupController(DatabaseContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] Group group)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        return Ok(group);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroup(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .Include(g => g.Messages).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null) return NotFound();
        return Ok(group);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] int userId)
    {
        if (!await _context.GroupMembers.AnyAsync(m => m.GroupId == id && m.UserId == userId))
        {
            _context.GroupMembers.Add(new GroupMember { GroupId = id, UserId = userId });
            await _context.SaveChangesAsync();
            // notify the user they were added to the group
            await _notificationService.SendToUser(userId, new { Type = "AddedToGroup", GroupId = id });
        }
        return Ok();
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        var member = await _context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == userId);
        if (member != null)
        {
            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var messages = await _context.GroupMessages
            .Include(m => m.Sender)
            .Where(m => m.GroupId == id)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
        return Ok(messages);
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] GroupMessage message)
    {
        message.GroupId = id;
        message.Timestamp = DateTime.UtcNow;
        _context.GroupMessages.Add(message);
        await _context.SaveChangesAsync();
        // notify all group members (except sender) about new group message
        var memberIds = await _context.GroupMembers
            .Where(m => m.GroupId == id && m.UserId != message.SenderId)
            .Select(m => m.UserId)
            .ToListAsync();

        foreach (var memberId in memberIds)
        {
            await _notificationService.SendToUser(memberId, new { Type = "NewGroupMessage", GroupId = id, From = message.SenderId, MessageId = message.Id, Preview = (message.Content ?? string.Empty).Substring(0, Math.Min(200, (message.Content ?? string.Empty).Length)) });
        }

        return Ok(message);
    }

    /// <summary>
    /// Sends an image as base64 in a group chat.
    /// </summary>
    /// <param name="id">The ID of the group.</param>
    /// <param name="imageMessage">The group message containing base64 image data and mime type.</param>
    /// <returns>The created group message with image data.</returns>
    [HttpPost("{id}/messages/image")]
    public async Task<IActionResult> SendImage(int id, [FromBody] GroupMessage imageMessage)
    {
        imageMessage.GroupId = id;
        imageMessage.Timestamp = DateTime.UtcNow;
        _context.GroupMessages.Add(imageMessage);
        await _context.SaveChangesAsync();
        return Ok(imageMessage);
    }
}

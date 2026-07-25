using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Void.DTOs;
using Void.Services;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly GroupService _groupService;

    public GroupController(GroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Group name is required." });

        var currentUserId = GetCurrentUserId();


        try
        {
            var group = await _groupService.CreateGroupAsync(dto.Name, currentUserId, dto.MemberIds); return Ok(new { id = group.Id, name = group.Name });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = GetCurrentUserId();
        var groups = await _groupService.GetGroupsForUserAsync(userId);
        return Ok(groups.Select(g => new { id = g.Id, name = g.Name, memberCount = g.Members.Count }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroup(int id)
    {
        var group = await _groupService.GetGroupAsync(id);
        if (group == null) return NotFound(new { error = "Group not found." });

        return Ok(new
        {
            id = group.Id,
            name = group.Name,
            members = group.Members.Select(m => new
            {
                userId = m.UserId,
                userName = m.User?.UserName,
                displayName = m.User?.DisplayName,
                profilePicture = m.User?.ProfilePicture
            })
        });
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddGroupMemberDTO dto)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            await _groupService.AddMemberAsync(id, currentUserId, dto.UserId);

            return Ok(new { message = "Member added." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        await _groupService.RemoveMemberAsync(id, userId);
        return Ok(new { message = "Member removed." });
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var messages = await _groupService.GetMessagesAsync(id);
        return Ok(messages);
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendGroupMessageDTO dto)
    {
        var senderId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(dto.Content) && string.IsNullOrWhiteSpace(dto.ImageData))
            return BadRequest(new { error = "Message content or image is required." });

        var errors = new List<string>();
        MessageImageValidator.Validate(dto.ImageData, dto.ImageMimeType, errors);
        if (errors.Any())
            return BadRequest(new { error = string.Join(Environment.NewLine, errors) });

        try
        {
            var result = await _groupService.SendMessageAsync(id, senderId, dto.Content ?? string.Empty, dto.ImageData, dto.ImageMimeType);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            if (ex.Message == "Group not found.")
                return NotFound(new { error = ex.Message });

            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    [HttpPost("{id}/messages/image")]
    public async Task<IActionResult> SendImage(int id, [FromBody] SendGroupMessageDTO dto)
    {
        var senderId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(dto.ImageData))
            return BadRequest(new { error = "Image data is required." });

        var errors = new List<string>();
        MessageImageValidator.Validate(dto.ImageData, dto.ImageMimeType, errors);
        if (errors.Any())
            return BadRequest(new { error = string.Join(Environment.NewLine, errors) });

        try
        {
            var result = await _groupService.SendMessageAsync(id, senderId, string.Empty, dto.ImageData, dto.ImageMimeType);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            if (ex.Message == "Group not found.")
                return NotFound(new { error = ex.Message });

            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("User not authenticated.");
        return id;
    }
}

public class SendGroupMessageDTO
{
    public string? Content { get; set; }
    public string? ImageData { get; set; }
    public string? ImageMimeType { get; set; }
}

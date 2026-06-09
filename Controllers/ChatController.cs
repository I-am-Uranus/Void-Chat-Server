using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Void.DTOs;
using Void.Services;

[ApiController]
[Route("api/chats")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("conversation")]
    public async Task<IActionResult> GetConversation([FromQuery] int user1, [FromQuery] int user2)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId != user1 && currentUserId != user2)
            return Forbid();

        var chats = await _chatService.GetConversationAsync(user1, user2);
        return Ok(chats);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatCreateDTO chatDto)
    {
        chatDto.SenderId = GetCurrentUserId();
        var result = await _chatService.SendMessageAsync(chatDto);
        return Ok(result);
    }

    [HttpPost("image/{user1}/{user2}")]
    public async Task<IActionResult> SendImage(int user1, int user2, [FromBody] ChatCreateDTO chatDto)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId != user1 && currentUserId != user2)
            return Forbid();

        chatDto.SenderId = currentUserId;
        chatDto.ReceiverId = currentUserId == user1 ? user2 : user1;
        var result = await _chatService.SendMessageAsync(chatDto);
        return Ok(result);
    }

    [HttpPost("seen/conversation")]
    public async Task<IActionResult> MarkConversationSeen([FromQuery] int otherUserId)
    {
        var viewerId = GetCurrentUserId();
        var count = await _chatService.MarkConversationAsSeen(viewerId, otherUserId);
        return Ok(new { seen = count });
    }

    [HttpPost("seen/message/{messageId}")]
    public async Task<IActionResult> MarkMessageSeen(int messageId)
    {
        var viewerId = GetCurrentUserId();
        var ok = await _chatService.MarkMessageAsSeen(messageId, viewerId);
        if (!ok) return NotFound(new { message = "Message not found or not authorized" });
        return Ok(new { message = "Message marked as seen" });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
            throw new UnauthorizedAccessException("User not authenticated.");
        return id;
    }
}

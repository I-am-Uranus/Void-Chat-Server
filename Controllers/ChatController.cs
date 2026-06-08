using Microsoft.AspNetCore.Mvc;
using Void.DTOs;
using Void.Services;

[ApiController]
[Route("api/chats")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("conversation")]
    public async Task<IActionResult> GetConversation(int user1, int user2)
    {
        var chats = await _chatService.GetConversationAsync(user1, user2);
        return Ok(chats);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatCreateDTO chatDto)
    {
        var result = await _chatService.SendMessageAsync(chatDto);
        return Ok(result);
    }

    /// <summary>
    /// Sends an image as base64 in a private chat.
    /// </summary>
    /// <param name="user1">The ID of the first user (sender).</param>
    /// <param name="user2">The ID of the second user (receiver).</param>
    /// <param name="chatDto">The chat message DTO containing base64 image data.</param>
    /// <returns>The created chat message with image data.</returns>
    [HttpPost("image/{user1}/{user2}")]
    public async Task<IActionResult> SendImage(int user1, int user2, [FromBody] ChatCreateDTO chatDto)
    {
        chatDto.SenderId = user1;
        chatDto.ReceiverId = user2;
        var result = await _chatService.SendMessageAsync(chatDto);
        return Ok(result);
    }
}

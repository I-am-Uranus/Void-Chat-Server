using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Void.DTOs;
using Void.Services;

namespace Void.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FriendshipController : ControllerBase
    {
        private readonly FriendshipService _friendshipService;

        public FriendshipController(FriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                throw new UnauthorizedAccessException("User not authenticated");
            return userId;
        }

        [HttpPost("request")]
        public async Task<IActionResult> SendFriendRequest([FromBody] FriendshipRequestDTO request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var friendship = await _friendshipService.SendFriendRequest(userId, request.FriendId);

                return Ok(new { message = "Friend request sent", friendshipId = friendship.Id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }


        [HttpPost("respond")]
        public async Task<IActionResult> RespondToFriendRequest([FromBody] FriendshipResponseDTO response)
        {
            try
            {
                var userId = GetCurrentUserId();
                var friendship = await _friendshipService.RespondToFriendRequest(response.FriendshipId, userId, response.Accept);

                var message = response.Accept ? "Friend request accepted" : "Friend request declined";
                return Ok(new { message, friendshipId = friendship.Id });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var userId = GetCurrentUserId();
            var friends = await _friendshipService.GetFriends(userId);
            return Ok(friends);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = GetCurrentUserId();
            var requests = await _friendshipService.GetPendingRequests(userId);
            return Ok(requests);
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var userId = GetCurrentUserId();
            var result = await _friendshipService.RemoveFriend(userId, friendId);

            if (!result)
                return NotFound("Friend relationship not found");

            return Ok(new { message = "Friend removed" });
        }

        [HttpPost("block/{userIdToBlock}")]
        public async Task<IActionResult> BlockUser(int userIdToBlock)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _friendshipService.BlockUser(userId, userIdToBlock);
                return Ok(new { message = "User blocked successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

}

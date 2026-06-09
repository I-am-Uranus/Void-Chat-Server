using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Void.DTOs;
using Void.Services;

namespace Void.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
                var friendRequest = await _friendshipService.SendFriendRequest(userId, request.FriendId);

                return Ok(new { message = "Friend request sent", friendRequestId = friendRequest.Id });
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
                var requestId = response.RequestId != 0 ? response.RequestId : response.FriendshipId;
                var friendRequest = await _friendshipService.RespondToFriendRequest(requestId, userId, response.Accept);

                var message = response.Accept ? "Friend request accepted" : "Friend request declined";
                return Ok(new { message, friendRequestId = friendRequest.Id });
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
        [HttpGet("friends")]
        [HttpGet("friendsList")]
        [HttpGet("list")]
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

        [HttpGet("blocked")]
        public async Task<IActionResult> GetBlockedUsers()
        {
            var userId = GetCurrentUserId();
            var blockedUsers = await _friendshipService.GetBlockedUsers(userId);
            return Ok(blockedUsers);
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

        [HttpPost("unblock/{userIdToUnblock}")]
        public async Task<IActionResult> UnblockUser(int userIdToUnblock)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _friendshipService.UnblockUser(userId, userIdToUnblock);
                if (!result)
                    return NotFound(new { message = "Block not found" });

                return Ok(new { message = "User unblocked successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
        }
    }

}

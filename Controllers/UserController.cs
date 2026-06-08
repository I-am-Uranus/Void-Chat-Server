using Microsoft.AspNetCore.Mvc;
using Void.Models;
using Void.Services;


namespace Void.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public ActionResult<List<User>> GetAll()
        {
            return Ok(_userService.GetAll());
        }


        [HttpGet("{id}")]
        public ActionResult<User> GetById(int id)
        {
            var user = _userService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }


        [HttpGet("search")]
        public ActionResult<List<User>> FindByName([FromQuery] string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Ok(new List<User>());
            }

            return Ok(_userService.FindByName(userName));
        }


        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] User updateUser)
        {
            if (updateUser == null || id != updateUser.Id)
            {
                return BadRequest("Invalid user data or ID mismatch.");
            }


            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!_userService.Update(id, updateUser))
            {
                return NotFound();
            }

            return NoContent();
        }


        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                _userService.Delete(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
        [HttpPost("heartbeat/{userId}")]
        public IActionResult Heartbeat(int userId)
        {
            _userService.MarkUserActive(userId);
            return Ok();
        }





    }
}

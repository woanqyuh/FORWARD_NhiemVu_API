using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ForwardMessage.Models;
using ForwardMessage.Services;

using System.Security.Claims;
using MongoDB.Bson;

namespace ForwardMessage.Controllers
{
    [ApiController]
    [Route("api/chatgroup")]
    [Authorize]
    public class ChatGroupController : ControllerBase
    {
        private readonly IChatGroupService _chatGroupService;

        public ChatGroupController(IChatGroupService chatGroupService)
        {
            _chatGroupService = chatGroupService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {

                var response = await _chatGroupService.GetAll();
                if (!response.IsOk)
                {
                    return StatusCode(response.StatusCode, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)StatusCodeEnum.InternalServerError, new { message = ex.Message });
            }
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] ChatGroupRequest model)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;

                var response = await _chatGroupService.CreateAsync(model, ObjectId.Parse(userIdClaim));
                if (!response.IsOk)
                {
                    return StatusCode(response.StatusCode, response);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)StatusCodeEnum.InternalServerError, new { message = ex.Message });
            }
        }


        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ChatGroupRequest model)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                var response = await _chatGroupService.UpdateAsync(ObjectId.Parse(id), model, ObjectId.Parse(userIdClaim),int.Parse(userRoleClaim));
                if (!response.IsOk)
                {
                    return StatusCode(response.StatusCode, response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode((int)StatusCodeEnum.InternalServerError, new { message = ex.Message });
            }
        }
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                var response = await _chatGroupService.DeleteAsync(ObjectId.Parse(id), ObjectId.Parse(userIdClaim), int.Parse(userRoleClaim));
                if (!response.IsOk)
                {
                    return StatusCode(response.StatusCode, response);
                }

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode((int)StatusCodeEnum.InternalServerError, new { message = ex.Message });
            }
        }
    }
}

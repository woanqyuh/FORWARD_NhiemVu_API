using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ForwardMessage.Models;
using ForwardMessage.Services;

using System.Security.Claims;
using MongoDB.Bson;
using Telegram.Bot.Types;

namespace ForwardMessage.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UserController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var response = await _userService.GetUsers();
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
        public async Task<IActionResult> Create([FromBody] RegisterModel model)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                if (int.TryParse(userRoleClaim, out int userRoleInt) && userRoleInt == (int)UserRole.User)
                {
                    return StatusCode((int)StatusCodeEnum.Forbidden, new { message = "Bạn không có quyền" });

                }
                var response = await _userService.CreateUserAsync(model, ObjectId.Parse(userIdClaim));
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
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserModel model)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                if (int.TryParse(userRoleClaim, out int userRoleInt) && userRoleInt == (int)UserRole.User)
                {
                    return StatusCode((int)StatusCodeEnum.Forbidden, new { message = "Bạn không có quyền" });

                }

                var response = await _userService.UpdateUserAsync(ObjectId.Parse(id), model);
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

                if (int.TryParse(userRoleClaim, out int userRoleInt) && userRoleInt == (int)UserRole.User)
                {
                    return StatusCode((int)StatusCodeEnum.Forbidden, new { message = "Bạn không có quyền" });

                }

                var response = await _userService.DeleteUserAsync(ObjectId.Parse(id));
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
    }
}

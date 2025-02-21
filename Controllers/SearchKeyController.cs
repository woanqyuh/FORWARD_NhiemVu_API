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
    [Route("api/keys")]
    public class SearchKeyController : ControllerBase
    {
        private readonly IKeyService _keyService;
        private readonly IAuthService _authService;

        public SearchKeyController(IKeyService keyService, IAuthService authService)
        {
            _keyService = keyService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _keyService.GetKeys();
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
        public async Task<IActionResult> Create([FromBody] KeyRequest model)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;

                var response = await _keyService.CreateAsync(model, ObjectId.Parse(userIdClaim));
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
        public async Task<IActionResult> Update(string id, [FromBody] KeyRequest model)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;

                var response = await _keyService.UpdateAsync(ObjectId.Parse(id), model);
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


                var response = await _keyService.DeleteAsync(ObjectId.Parse(id));
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

using Microsoft.AspNetCore.Mvc;
using ForwardMessage.Models;
using ForwardMessage.Services;

namespace ForwardMessage.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
  
        public AuthController(
            IAuthService authService,
            IUserService userService,
            IConfiguration configuration


            )
        {
            _authService = authService;
            _userService = userService;
            _configuration = configuration;
  
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {

            try
            {

                var response = await _authService.LoginAsync(model);

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
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeModel model)
        {
            try
            {

                var response = await _authService.VerifyCode(model);

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

        [HttpPost("refresh-token")]

        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest model)
        {

            try
            {

                var response = await _authService.RefreshTokenAsync(model);

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
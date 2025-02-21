using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ForwardMessage.Models;
using ForwardMessage.Services;
using MongoDB.Bson;
using System.Security.Claims;
using Telegram.Bot.Types;
using System.Text.Json;
using Telegram.Bot;
using System.Net.Http;
using Telegram.Bot.Requests;
using Quartz;


namespace ForwardMessage.Controllers
{
    [ApiController]
    [Route("api/telebot")]
    [Authorize] 
    public class TeleBotController : ControllerBase
    {
        private readonly ITelegramBotService _telegramBotService;
        private readonly IChatGroupService _chatGroupService;
        private readonly ITaskService _taskService;
        private readonly ITelegramBotClient _botClient;
        private readonly ISchedulerFactory _schedulerFactory;
        public TeleBotController(
            ITelegramBotService telegramBotService,
            IChatGroupService chatGroupService,
            ITaskService taskService,
            ITelegramBotClient botClient,
            ISchedulerFactory schedulerFactory)
        {
            _telegramBotService = telegramBotService;
            _chatGroupService = chatGroupService;
            _taskService = taskService;
            _botClient = botClient;
            _schedulerFactory = schedulerFactory;
        }

      
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] TelebotModel model)
        {
            try
            {
                var response = await _telegramBotService.SendMessageTaskAsync(model, ObjectId.Parse(User.FindFirst(ClaimTypes.Name)?.Value));
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

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Post([FromBody] JsonElement payload)
        {
            try
            {
                if (payload.ValueKind == JsonValueKind.Object)
                {
                    if (payload.TryGetProperty("message", out var message))
                    {

                        if (message.TryGetProperty("chat", out var chat) &&
                            chat.TryGetProperty("id", out var chatId))
                        {
                            var chatIdValue = chatId.GetInt64();

                            if (message.TryGetProperty("text", out var text))
                            {
                                var messageText = text.GetString();

                                if (!string.IsNullOrEmpty(messageText) && (messageText.ToLower() == "/getchatid" || messageText.ToLower() == "/start"))
                                {
                                     var sentMessage = await _botClient.SendMessage(
                                        chatIdValue,
                                        $"Xin chào! ChatId của bạn là: {chatIdValue}"
                                    );

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode((int)StatusCodeEnum.InternalServerError, new { message = ex.Message });
            }

            return Ok();
        }
        [HttpGet("get-tasks")]
        public async Task<IActionResult> GetAllTask([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var response = await _taskService.GetAll(fromDate, toDate);
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
        [HttpPost("sync-sheet")]
        public async Task<IActionResult> TriggerSyncJob()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey("SyncGoogleSheetsJob");

            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.TriggerJob(jobKey);
                return Ok("SyncGoogleSheetsJob triggered successfully!");
            }

            return NotFound("Job not found!");
        }
    }
}

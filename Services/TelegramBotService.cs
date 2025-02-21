using ForwardMessage.Dtos;
using ForwardMessage.Models;
using ForwardMessage.Repositories;
using MongoDB.Bson;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;


namespace ForwardMessage.Services
{
    public interface ITelegramBotService
    {
        Task<ApiResponse<object>> SendMessageTaskAsync(TelebotModel model, ObjectId userId);
    }
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IChatGroupRepository _chatGroupRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IUserRepository _userRepository;

        public TelegramBotService(
            IConfiguration configuration, 
            IChatGroupRepository chatGroupRepository, 
            ITaskRepository taskRepository, 
            ITelegramBotClient botClient, 
            IUserRepository userRepository)
        {
            _botClient = botClient;
            _chatGroupRepository = chatGroupRepository;
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        public async Task<ApiResponse<object>> SendMessageTaskAsync(TelebotModel model, ObjectId userId)
        {
            TaskDto taskDto;

            if (!string.IsNullOrEmpty(model.Id))
            {
                if (!ObjectId.TryParse(model.Id, out ObjectId objectId))
                {
                    return ApiResponse<object>.Fail("Id không hợp lệ",StatusCodeEnum.Invalid);
                }
                var existingTask = await _taskRepository.GetByIdAsync(objectId);
                if (existingTask == null)
                {
                    return ApiResponse<object>.Fail("Task not found", StatusCodeEnum.NotFound);
                }

                taskDto = existingTask;
                taskDto.Status = TaskStatus.Sending;
                try
                {
                    taskDto.SearchKey = model.SearchKey;
                    taskDto.ChatId = model.ChatId;
                    //taskDto.CompetitorLink = model.CompetitorLink;
                    //taskDto.TargetLink = model.TargetLink;
                    taskDto.ImageUrl = model.ImageUrl;
                    taskDto.Content = model.Content;
                    taskDto.SendTime = DateTime.UtcNow;
                    await _taskRepository.UpdateAsync(taskDto.Id, taskDto);
                }
                catch (Exception ex)
                {
                    return ApiResponse<object>.Fail($"Failed to update task status: {ex.Message}", StatusCodeEnum.InternalServerError);
                }
            }
            else
            {

                taskDto = new TaskDto
                {
                    Id = ObjectId.GenerateNewId(),
                    SearchKey = model.SearchKey,
                    ChatId = model.ChatId,
                    //CompetitorLink = model.CompetitorLink,
                    //TargetLink = model.TargetLink,
                    ImageUrl = model.ImageUrl,
                    Status = TaskStatus.Sending,
                    CreatedBy = userId,
                    Content = model.Content,
                };
                try
                {
                    await _taskRepository.AddAsync(taskDto);
                }
                catch (Exception ex)
                {
                    return ApiResponse<object>.Fail($"Failed to create task: {ex.Message}", StatusCodeEnum.InternalServerError);
                }
            }

            try
            {
                using var client = new HttpClient();
                var user = await _userRepository.GetByIdAsync(userId);
                var currentDateTime = DateTime.Now;
                var currentTime = currentDateTime.TimeOfDay;

                var successChatIds = new List<string>();
                var errorChatIds = new List<ErrorSendResponse>();

                foreach (var chatId in model.ChatId)
                {
                    var chatGroup = await _chatGroupRepository.GetByChatId(chatId);
                    if (chatGroup == null)
                    {
                        errorChatIds.Add(new ErrorSendResponse { ChatId = chatId, ChatError = "ChatId không tồn tại", ChatName = "N/A" });
                        continue;
                    }
                    if (currentTime < chatGroup.WorkStartTime || currentTime > chatGroup.WorkEndTime )
                    {
                        errorChatIds.Add(new ErrorSendResponse { ChatId = chatId,ChatError = "Ngoài ca làm việc",ChatName = chatGroup.ChatName });
                        continue;
                    }
                    var cleanContent = ConvertHtmlToTelegramMessage(model.Content);
                    var formattedDateTime = currentDateTime.ToString("dd/MM/yyyy HH'h'mm");
                    var message = $"👉🏼 NHIỆM VỤ NGÀY {formattedDateTime}\n" +
                                  $"👉🏼 NGƯỜI THỰC HIỆN: {user.Fullname}\n\n" +
                                   $"{cleanContent}";
                                  

                    //              $"🔎🔎🌎 VÀO TRÌNH DUYỆT TÌM KIẾM KEY SAU: {model.SearchKey}\n\n" +
                    //              $"💢 LINK QUẢNG CÁO (LINK ĐỐI THỦ):\n{model.CompetitorLink ?? "N/A"}\n\n";

                    //if (!string.IsNullOrWhiteSpace(model.TargetLink))
                    //{
                    //    message += $"🎯🎯🎯LINK TRANG CHUYỂN ĐỔI (TRANG MẠO DANH):\n{model.TargetLink}\n\n";
                    //}

                    //message += $"⏩⏩⏩ NỘI DUNG YÊU CẦU THAO TÁC:\n{model.Content}";

                    try
                    {
                        if (!string.IsNullOrEmpty(model.ImageUrl) && Uri.IsWellFormedUriString(model.ImageUrl, UriKind.Absolute))
                        {
                            var responsec = await client.GetAsync(model.ImageUrl);

                            if (responsec.IsSuccessStatusCode)
                            {
                                var stream = await responsec.Content.ReadAsStreamAsync();
                                await _botClient.SendPhoto(chatId, stream, message);
                                successChatIds.Add(chatId);
                                continue;
                            }
                        }
                        await _botClient.SendMessage(chatId, message);
                        successChatIds.Add(chatId);
                    }
                    catch (ApiRequestException ex){
                        errorChatIds.Add(new ErrorSendResponse { ChatId = chatId, ChatError = ex.Message, ChatName = chatGroup.ChatName });
                        continue;
                    }
                    catch (Exception ex) {
                        errorChatIds.Add(new ErrorSendResponse { ChatId = chatId, ChatError = ex.Message, ChatName = chatGroup.ChatName });
                        continue;
                    }
                }

                taskDto.Status = TaskStatus.Completed;
                await _taskRepository.UpdateAsync(taskDto.Id, taskDto);
                var result = new
                {
                    SuccessChatIds = successChatIds,
                    ErrorChatIds = errorChatIds
                };

                return ApiResponse<object>.Success(result, "Send Success");
            }  
            catch (Exception ex)
            {
                taskDto.Status = TaskStatus.Failed;
                await _taskRepository.UpdateAsync(taskDto.Id, taskDto);
                return ApiResponse<object>.Fail($"Unexpected error: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }

        string ConvertHtmlToTelegramMessage(string htmlContent)
        {

            string decoded = WebUtility.HtmlDecode(htmlContent);

 
            decoded = Regex.Replace(decoded, @"<br\s*/?>", "\n");
            decoded = Regex.Replace(decoded, @"</p>", "\n");
            decoded = Regex.Replace(decoded, @"<p>", "");

 
            decoded = Regex.Replace(decoded, "<.*?>", string.Empty);

            return decoded.Trim();
        }

    }
}

using ForwardMessage.Models;
using ForwardMessage.Repositories;
using ForwardMessage.Dtos;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;

namespace ForwardMessage.Services
{
    public interface ITaskService
    {
        Task<ApiResponse<List<TaskModel>>> GetAll(DateTime? fromDate, DateTime? toDate);
    }

    public class TaskService : ITaskService
    {
        private readonly IMapper _mapper;
        private readonly ITaskRepository _taskRepository;
        private readonly IAuthService _authService;
        private readonly IChatGroupRepository _chatGroupRepository;
        private readonly IUserRepository _userRepository;

        public TaskService(IAuthService authService, IMapper mapper, ITaskRepository taskRepository, IChatGroupRepository chatGroupRepository, IUserRepository userRepository)
        {
            _authService = authService;
            _mapper = mapper;
            _taskRepository = taskRepository;
            _chatGroupRepository = chatGroupRepository;
            _userRepository = userRepository;
        }

        public async Task<ApiResponse<List<TaskModel>>> GetAll(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var taskListDto = await _taskRepository.GetAllAsync();
                if (fromDate.HasValue)
                {
                    taskListDto = taskListDto.Where(t => t.CreatedAt >= fromDate.Value).ToList();
                }

                if (toDate.HasValue)
                {
                    taskListDto = taskListDto.Where(t => t.CreatedAt <= toDate.Value).ToList();
                }

                var taskListReturn = _mapper.Map<List<TaskModel>>(taskListDto);
                var chatGroupDtos = new List<ChatGroupDto>();

   
                foreach (var task in taskListReturn)
                {
                    if (task.ChatId != null && task.ChatId.Any())
                    {
                        var chatGroupTasks = task.ChatId.Select(chatId => _chatGroupRepository.GetByChatId(chatId)).ToArray();
                        var chatGroups = await Task.WhenAll(chatGroupTasks);
                        task.ChatGroup = _mapper.Map<List<ChatGroup>>(chatGroups);
                    }

                    if (!string.IsNullOrEmpty(task.CreatedBy))
                    {
                        var createdUser = await _userRepository.GetByIdAsync(ObjectId.Parse(task.CreatedBy));
                        task.CreatedName = createdUser?.Fullname ?? "Không xác định";
                    }
                }

                return ApiResponse<List<TaskModel>>.Success(taskListReturn, "Thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<TaskModel>>.Fail($"Đã xảy ra lỗi: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }
    }
}

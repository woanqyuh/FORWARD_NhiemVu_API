using ForwardMessage.Models;
using ForwardMessage.Repositories;
using ForwardMessage.Dtos;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ForwardMessage.Services
{
    public interface IChatGroupService
    {
        Task<ApiResponse<List<ChatGroup>>> GetAll();
        Task<ApiResponse<ChatGroup>> CreateAsync(ChatGroupRequest model, ObjectId userId);
        Task<ApiResponse<ChatGroup>> UpdateAsync(ObjectId id, ChatGroupRequest model, ObjectId userId ,int role);
        Task<ApiResponse<string>> DeleteAsync(ObjectId id, ObjectId userId, int role);
    }

    public class ChatGroupService : IChatGroupService
    {
        private readonly IMapper _mapper;
        private readonly IChatGroupRepository _chatGroupRepository;
        private readonly IUserRepository _userRepository;

        public ChatGroupService(IMapper mapper, IChatGroupRepository chatGroupRepository, IUserRepository userRepository)
        {
            _mapper = mapper;
            _chatGroupRepository = chatGroupRepository;
            _userRepository = userRepository;
        }

        public async Task<ApiResponse<List<ChatGroup>>> GetAll()
        {
            try
            {
                var chatListDto = await _chatGroupRepository.GetAllAsync();
                var chatListReturn = _mapper.Map<List<ChatGroup>>(chatListDto);

                foreach (var chat in chatListReturn)
                {
                    if (!string.IsNullOrEmpty(chat.CreatedBy))
                    {
                        var createdUser = await _userRepository.GetByIdAsync(ObjectId.Parse(chat.CreatedBy));
                        chat.CreatedName = createdUser?.Fullname ?? "Không xác định";
                    }
                }
                return ApiResponse<List<ChatGroup>>.Success(chatListReturn, "Thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ChatGroup>>.Fail($"Đã xảy ra lỗi: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }

        public async Task<ApiResponse<ChatGroup>> CreateAsync(ChatGroupRequest model, ObjectId userId)
        {
            try
            {
                var existingUser = await _chatGroupRepository.GetByChatId(model.ChatId);
                if (existingUser != null )
                {
                    return ApiResponse<ChatGroup>.Fail($"Chat Id '{model.ChatId}' đã tồn tại", StatusCodeEnum.Invalid);
                }

                var startTime = TimeSpan.Parse(model.WorkStartTime);
                var endTime = TimeSpan.Parse(model.WorkEndTime);

                if (startTime >= endTime)
                {
                    return ApiResponse<ChatGroup>.Fail($"Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.", StatusCodeEnum.Invalid);
                }

                var chat = new ChatGroupDto
                {
                    Id = ObjectId.GenerateNewId(),
                    ChatId = model.ChatId,
                    ChatName = model.ChatName,
                    CreatedBy = userId,
                    WorkEndTime = endTime,
                    WorkStartTime = startTime,
                };
                await _chatGroupRepository.AddAsync(chat);

                var chatGroupReturn = _mapper.Map<ChatGroup>(chat);

                return ApiResponse<ChatGroup>.Success(chatGroupReturn, "Tạo thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatGroup>.Fail($"Đã xảy ra lỗi: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }

        public async Task<ApiResponse<ChatGroup>> UpdateAsync(ObjectId id, ChatGroupRequest model, ObjectId userId, int role)
        {
            try
            {
                var chat = await _chatGroupRepository.GetByIdAsync(id);
                if (chat == null)
                {
                    return ApiResponse<ChatGroup>.Fail("Không tìm thấy nhóm trò chuyện", StatusCodeEnum.NotFound);
                }
                if(chat.CreatedBy != userId && role != (int)UserRole.Admin)
                {
                    return ApiResponse<ChatGroup>.Fail("Bạn không thể cập nhật nhóm này", StatusCodeEnum.Invalid);
                }
                var startTime = TimeSpan.Parse(model.WorkStartTime);
                var endTime = TimeSpan.Parse(model.WorkEndTime);

                if (startTime >= endTime)
                {
                    return ApiResponse<ChatGroup>.Fail($"Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.", StatusCodeEnum.Invalid);
                }

                chat.ChatId = model.ChatId;
                chat.ChatName = model.ChatName;
                chat.WorkEndTime = endTime;
                chat.WorkStartTime = startTime;

                await _chatGroupRepository.UpdateAsync(id, chat);

                return ApiResponse<ChatGroup>.Success(_mapper.Map<ChatGroup>(chat), "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatGroup>.Fail($"Đã xảy ra lỗi: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }

        public async Task<ApiResponse<string>> DeleteAsync(ObjectId id, ObjectId userId, int role)
        {
            try
            {
                var chat = await _chatGroupRepository.GetByIdAsync(id);
                if (chat == null)
                {
                    return ApiResponse<string>.Fail("Không tìm thấy nhóm trò chuyện", StatusCodeEnum.NotFound);
                }
                if (chat.CreatedBy != userId && role != (int)UserRole.Admin)
                {
                    return ApiResponse<string>.Fail("Bạn không thể cập nhật nhóm này", StatusCodeEnum.Invalid);
                }
                await _chatGroupRepository.DeleteAsync(id);
                return ApiResponse<string>.Success(null, "Xoá thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Fail($"Đã xảy ra lỗi: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }
    }
}

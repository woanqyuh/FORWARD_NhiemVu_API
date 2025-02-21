
using ForwardMessage.Models;
using ForwardMessage.Repositories;
using MongoDB.Bson;
using ForwardMessage.Dtos;
using AutoMapper;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;




namespace ForwardMessage.Services
{
    public interface IKeyService
    {

        Task<ApiResponse<List<KeyModel>>> GetKeys();
        Task<ApiResponse<KeyModel>> CreateAsync(KeyRequest model, ObjectId userId);
        Task<ApiResponse<KeyModel>> UpdateAsync(ObjectId id, KeyRequest model);
        Task<ApiResponse<string>> DeleteAsync(ObjectId id);
    }

    public class KeyService : IKeyService
    {
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IKeyRepository _keyRepository;


        public KeyService(IUserRepository userRepository, IKeyRepository keyRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _keyRepository = keyRepository;
            _mapper = mapper;
        }
        public async Task<ApiResponse<List<KeyModel>>> GetKeys()
        {
            try
            {
                var keyListDto = await _keyRepository.GetAllAsync();
                var keyListReturn = _mapper.Map<List<KeyModel>>(keyListDto);

                foreach (var key in keyListReturn)
                {
                    if (!string.IsNullOrEmpty(key.CreatedBy))
                    {
                        var createdUser = await _userRepository.GetByIdAsync(ObjectId.Parse(key.CreatedBy));
                        key.CreatedName = createdUser?.Fullname ?? "Không xác định";
                    }
                }
                return ApiResponse<List<KeyModel>>.Success(keyListReturn, "Thành công");

            }
            catch (Exception ex)
            {
                return ApiResponse<List<KeyModel>>.Fail($"An unexpected error occurred: {ex.Message}", StatusCodeEnum.InternalServerError);
            }

        }
        public async Task<ApiResponse<KeyModel>> CreateAsync(KeyRequest model, ObjectId userId)
        {
            try
            {
                var existingKey = await _keyRepository.GetBySearchKeyAsync(model.SearchKey);
                if (existingKey != null)
                {
                    return ApiResponse<KeyModel>.Fail($"Searchkey '{model.SearchKey}' already exists.", StatusCodeEnum.Invalid);
                }
                var key = new KeyDto
                {
                    Id = ObjectId.GenerateNewId(),
                    SearchKey = model.SearchKey,
                    CreatedBy = userId,

                };
                await _keyRepository.AddAsync(key);

                var keyModel = _mapper.Map<KeyModel>(key);
                return ApiResponse<KeyModel>.Success(keyModel, "Key created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<KeyModel>.Fail($"An unexpected error occurred: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }
        public async Task<ApiResponse<KeyModel>> UpdateAsync(ObjectId id, KeyRequest model)
        {
            try
            {
                var key = await _keyRepository.GetByIdAsync(id);

                if (key == null)
                {
                    return ApiResponse<KeyModel>.Fail("Search key not found", StatusCodeEnum.NotFound);
                }

                key.SearchKey = model.SearchKey;
                await _keyRepository.UpdateAsync(id, key);
                var keyModel = _mapper.Map<KeyModel>(key);
                return ApiResponse<KeyModel>.Success(keyModel, "Search key updated successfully");
            }
            catch (Exception ex)
            {

                return ApiResponse<KeyModel>.Fail($"An unexpected error occurred: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }
        public async Task<ApiResponse<string>> DeleteAsync(ObjectId id)
        {
            try
            {
                var user = await _keyRepository.GetByIdAsync(id);

                if (user == null)
                {
                    return ApiResponse<string>.Fail("Search key not found", StatusCodeEnum.NotFound);
                }
                await _keyRepository.DeleteAsync(id);

   
                return ApiResponse<string>.Success(null, "Search key deleted successfully");
            }
            catch (Exception ex)
            {

                return ApiResponse<string>.Fail($"An unexpected error occurred: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }

    }

}

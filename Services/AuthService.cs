using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using ForwardMessage.Models;
using MongoDB.Bson;
using ForwardMessage.Repositories;
using Telegram.Bot;
using Microsoft.Extensions.Caching.Memory;
using ForwardMessage.Dtos;

namespace ForwardMessage.Services
{
    public interface IAuthService
    {

        Task<ApiResponse<AuthResponse>> RefreshTokenAsync(TokenRequest model);
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginModel model);
        string HashPassword(string password);
        Task<ApiResponse<AuthResponse>> VerifyCode(VerifyCodeModel model);
        string GetBotLink();
    }
    public class AuthService : IAuthService

    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _codeExpiry = TimeSpan.FromMinutes(2);
        private readonly int _maxAttempts = 5;
        private readonly TelegramBotClient _botClient;

        private readonly string _accessTokenSecret;
        private readonly string _refreshTokenSecret;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;
        private readonly IUserRepository _userRepository;
        private readonly ISheetRepository _sheetRepository;

        public AuthService(
            IMemoryCache cache,
            IConfiguration configuration,
            IUserRepository userRepository,

            ISheetRepository sheetRepository)
        {
            _botClient = new TelegramBotClient(configuration.GetValue<string>("TelegramBot:ApiKey"));
            _userRepository = userRepository;
            _accessTokenSecret = configuration.GetValue<string>("JWT:AccessTokenSecret");
            _refreshTokenSecret = configuration.GetValue<string>("JWT:RefreshTokenSecret");
            _accessTokenExpirationMinutes = configuration.GetValue<int>("JWT:AccessTokenExpirationMinutes");
            _refreshTokenExpirationDays = configuration.GetValue<int>("JWT:RefreshTokenExpirationDays");
            _cache = cache;
            _sheetRepository = sheetRepository;
        }



        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginModel model)
        {
            try
            {
                model.Username = model.Username.Trim().ToLower();
                var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
                if (existingUser == null)
                {
                    return ApiResponse<LoginResponse>.Fail($"Người dùng với tên đăng nhập '{model.Username}' không tồn tại.", StatusCodeEnum.NotFound);
                }

                var verifyPassword = VerifyPassword(model.Password, existingUser.Password);
                if (!verifyPassword)
                {
                    return ApiResponse<LoginResponse>.Fail("Sai mật khẩu.", StatusCodeEnum.Invalid);
                }

                var sheetInfo = await _sheetRepository.GetByUsernameAsync(existingUser.TeleUser);
                if (sheetInfo != null)
                {
                    // Gửi mã xác thực qua Telegram
                    await SendVerificationCodeAsync(sheetInfo.ChatId, existingUser.Id.ToString());

                    var responseData = new LoginResponse
                    {
                        UserId = existingUser.Id.ToString(),
                        ChatId = sheetInfo.ChatId
                    };

                    return ApiResponse<LoginResponse>.Success(responseData, "Mã xác thực đã được gửi qua Telegram. Vui lòng kiểm tra.");
                }
                else
                {
                    return ApiResponse<LoginResponse>.Fail($"Người dùng '{existingUser.Username}' không tồn tại trong danh sách dữ liệu.", StatusCodeEnum.NotFound);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Forbidden: bot can't initiate conversation with a user"))
                {
                    var botInfo = _botClient.GetMe().Result;
                    if (botInfo != null && !string.IsNullOrEmpty(botInfo.Username))
                    {
                        return ApiResponse<LoginResponse>.Fail($"Vui lòng liên hệ với bot: <a href='https://t.me/{botInfo.Username}' target='_blank'>@{botInfo.Username}</a> và '/start' rồi thử lại!", StatusCodeEnum.Forbidden);
                    }

                }
                return ApiResponse<LoginResponse>.Fail($"An unexpected error occurred: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }


        public async Task<ApiResponse<AuthResponse>> VerifyCode(VerifyCodeModel model)
        {
            try
            {
                var response = new ApiResponse<AuthResponse>();
                var cacheKey = GenerateCacheKey(model.ChatId, model.UserId);

                if (!_cache.TryGetValue(cacheKey, out VerificationCacheEntry cacheEntry))
                {
                    return ApiResponse<AuthResponse>.Fail("Mã xác thực đã hết hạn hoặc không tồn tại.", StatusCodeEnum.Invalid);
                }

                if (cacheEntry.Attempts >= _maxAttempts)
                {
                    return ApiResponse<AuthResponse>.Fail("Bạn đã thử quá số lần cho phép. Vui lòng thử lại sau.", StatusCodeEnum.TooManyRequests);
                }
                if (model.Code != cacheEntry.Code)
                {

                    cacheEntry.Attempts++;
                    _cache.Set(cacheKey, cacheEntry, _codeExpiry);

                    return ApiResponse<AuthResponse>.Fail("Mã xác thực không chính xác.", StatusCodeEnum.Invalid);
                }

                if (cacheEntry.UserId != model.UserId)
                {
                    return ApiResponse<AuthResponse>.Fail("Người dùng không khớp với mã xác thực.", StatusCodeEnum.Invalid);
                }

 
                RemoveCode(model.ChatId, model.UserId);
                var user = await _userRepository.GetByIdAsync(ObjectId.Parse(model.UserId));
                if (user == null)
                {
                    return ApiResponse<AuthResponse>.Fail("Người dùng không tồn tại.", StatusCodeEnum.NotFound); 
                }
                response.Data = new AuthResponse
                {
                    AccessToken = GenerateAccessToken(user),
                    RefreshToken = GenerateRefreshToken(user),
                    User = new UserViewModel
                    {
                        Username = user.Username,
                        Fullname = user.Fullname,
                        CreatedAt = user.CreatedAt,
                        CreatedBy = user.CreatedBy.ToString(),
                        TeleUser = user.TeleUser,
                        Role = user.Role,
                    }
                };

                return ApiResponse<AuthResponse>.Success(response.Data, "Đăng nhập thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AuthResponse>.Fail($"Đã xảy ra lỗi: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }


        public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(TokenRequest model)
        {
            try
            {
                ClaimsPrincipal principal = GetPrincipalFromExpiredToken(model.RefreshToken);

                if (principal == null)
                {
                    return ApiResponse<AuthResponse>.Fail("Token không hợp lệ.", StatusCodeEnum.Unauthorized);
                }

                var userIdString = principal.Identity.Name;
                if (!ObjectId.TryParse(userIdString, out ObjectId userId))
                {
                    return ApiResponse<AuthResponse>.Fail("Token không hợp lệ.", StatusCodeEnum.Unauthorized);
                }

                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    return ApiResponse<AuthResponse>.Fail("Người dùng không tồn tại.", StatusCodeEnum.NotFound);
                }

                var accessToken = GenerateAccessToken(user);
                var refreshToken = GenerateRefreshToken(user);

                var authResponse = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    User = new UserViewModel
                    {
                        Username = user.Username,
                        Fullname = user.Fullname
                    }
                };

                return ApiResponse<AuthResponse>.Success(authResponse, "Thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AuthResponse>.Fail($"Đã xảy ra lỗi: {ex.Message}", StatusCodeEnum.InternalServerError);
            }
        }

        public string GenerateAccessToken(UserDto user)
        {
            return GenerateToken(user, Convert.ToInt32(_accessTokenExpirationMinutes), _accessTokenSecret);
        }
        public string GenerateRefreshToken(UserDto user)
        {
            return GenerateToken(user, Convert.ToInt32(_refreshTokenExpirationDays * 24 * 60), _refreshTokenSecret);
        }
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_refreshTokenSecret)),
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                return principal;
            }
            catch (SecurityTokenExpiredException)
            {

                throw new Exception("Token has expired.");
            }
            catch (SecurityTokenInvalidSignatureException)
            {

                throw new Exception("Token has an invalid signature.");
            }
            catch (Exception ex)
            {

                throw new Exception($"Token validation failed: {ex.Message}");
            }
        }
        public string GenerateToken(UserDto user, int expiresIn, string secret)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, ObjectId.GenerateNewId().ToString()),
            new Claim(ClaimTypes.Name, user.Id.ToString()), 
            new Claim(ClaimTypes.Role, ((int)user.Role).ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);



            var token = new JwtSecurityToken(
                issuer: "",
                audience: "your-app",
                claims: claims,
                expires: DateTime.Now.AddMinutes(expiresIn),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string HashPassword(string password)
        {

            byte[] salt = new byte[128 / 8];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt); // Lấy một salt ngẫu nhiên
            }


            string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));


            return Convert.ToBase64String(salt) + "." + hashedPassword;
        }
        public bool VerifyPassword(string enteredPassword, string storedHash)
        {

            var parts = storedHash.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[0]);
            string storedPasswordHash = parts[1];


            string enteredPasswordHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return storedPasswordHash == enteredPasswordHash;
        }
        private string GenerateCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        public async Task SendVerificationCodeAsync(string chatId, string userId)
        {
            var verificationCode = GenerateCode();
            var message = $"Mã xác thực của bạn là: {verificationCode}.\n Mã xác thực này chỉ có hiệu lực trong 2 phút";

            var sentMessage = await _botClient.SendMessage(chatId, message);

            SaveCode(chatId, userId, verificationCode);
        }
        private string GenerateCacheKey(string chatId, string userId) => $"VerificationCode_{chatId}_{userId}";

        public void SaveCode(string chatId, string userId, string code)
        {
            var cacheKey = GenerateCacheKey(chatId, userId);

            var cacheEntry = new VerificationCacheEntry
            {
                Code = code,
                Expiry = DateTime.UtcNow.Add(_codeExpiry),
                Attempts = 0,
                ChatId = chatId,
                UserId = userId
            };

            _cache.Set(cacheKey, cacheEntry, _codeExpiry);
        }

        public void RemoveCode(string chatId,string userId)
        {
            var cacheKey = GenerateCacheKey(chatId, userId);
            _cache.Remove(cacheKey);
        }

        public string GetBotLink()
        {
            try
            {
                var botInfo = _botClient.GetMe().Result;
                if (botInfo != null && !string.IsNullOrEmpty(botInfo.Username))
                {

                    return $"https://t.me/{botInfo.Username}";
                }

                return "Không thể lấy thông tin bot.";
            }
            catch (Exception ex)
            {
                return $"Đã xảy ra lỗi: {ex.Message}";
            }
        }
    }
}



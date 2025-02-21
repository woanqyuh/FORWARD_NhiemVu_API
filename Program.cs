using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ForwardMessage.Models;
using ForwardMessage.Dtos;
using ForwardMessage.Repositories;
using ForwardMessage.Services;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text;
using Quartz;
using Quartz.Impl;
using Telegram.Bot;



var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    // Đặt WebRootPath mới ở đây
    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "wwwroot")
});

// SignalR
builder.Services.AddSignalR();

// MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IMongoClient>(s =>
{
    var mongoDbSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
    return new MongoClient(mongoDbSettings.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(s =>
{
    var mongoClient = s.GetRequiredService<IMongoClient>();
    var mongoDbSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
    return mongoClient.GetDatabase(mongoDbSettings.DatabaseName);
});

// JWT
var jwtSettings = builder.Configuration.GetSection("JWT");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["AccessTokenSecret"])),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new TelegramBotClient(configuration["TelegramBot:ApiKey"]);
});

builder.Services.AddScoped<ITelegramBotService, TelegramBotService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IChatGroupService, ChatGroupService>();
builder.Services.AddScoped<IKeyService, KeyService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatGroupRepository, ChatGroupRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ISheetRepository, SheetRepository>();
builder.Services.AddScoped<IKeyRepository, KeyRepository>();
builder.Services.AddSingleton<GoogleSheetsHelper>();
builder.Services.AddTransient<SyncGoogleSheetsJob>();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("SyncGoogleSheetsJob");
    q.AddJob<SyncGoogleSheetsJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("SyncGoogleSheetsTrigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInMinutes(5) 
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT vào dưới dạng: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});




builder.Services.AddMemoryCache();


builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100MB
});

builder.Services.AddControllers();

var app = builder.Build();
var adminAccountSettings = builder.Configuration.GetSection("AdminAccount").Get<AdminAccountSettings>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(x => x
  .AllowAnyOrigin()
  .AllowAnyMethod()
  .AllowAnyHeader());

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userRepository = services.GetRequiredService<IUserRepository>();
    var authService = services.GetRequiredService<IAuthService>();

    var existingAdmin = await userRepository.GetByUsernameAsync(adminAccountSettings.Username);

    if (existingAdmin == null)
    {
        var hashedPassword = authService.HashPassword(adminAccountSettings.Password);

        var adminUser = new UserDto
        {
            Id = ObjectId.GenerateNewId(),
            Username = adminAccountSettings.Username,
            Password = hashedPassword,
            Fullname = adminAccountSettings.Fullname,
            Role = UserRole.Admin,
            TeleUser = adminAccountSettings.TeleUser
        };
        await userRepository.AddAsync(adminUser);

    }

    var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
    await botClient.SetWebhook(adminAccountSettings.Webhook);
}


app.UseHttpsRedirection();
app.UseStaticFiles();

//  Authentication  Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map  controller
app.MapControllers();

app.Run();

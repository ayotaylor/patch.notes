using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Backend.Services;
using Backend.Data;
using Backend.Config;
using Backend.Models.Auth;
using Backend.Services.Recommendation;
using Backend.Services.Recommendation.Interfaces;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IGameListService, GameListService>();
builder.Services.AddScoped<ICommentService, CommentService>();

// Add recommendation services
builder.Services.AddHttpClient<GroqLanguageModel>();
builder.Services.AddSingleton<QdrantClient>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var qdrantUrl = configuration["Qdrant:Url"] ?? "http://localhost:6333";
    var uri = new Uri(qdrantUrl);
    // Use gRPC port (6334) instead of HTTP port (6333) C# client uses gRPC interface by default
    var grpcPort = uri.Port == 6333 ? 6334 : uri.Port;
    return new QdrantClient(uri.Host, grpcPort, https: false);
});

builder.Services.AddScoped<IVectorDatabase, QdrantVectorDatabase>();
builder.Services.AddScoped<IEmbeddingService, SentenceTransformerEmbeddingService>();
builder.Services.AddScoped<ILanguageModel, GroqLanguageModel>();
builder.Services.AddScoped<UserPreferenceService>();
builder.Services.AddScoped<GameIndexingService>();
builder.Services.AddScoped<GameRecommendationService>();
builder.Services.AddSingleton<ConversationStateService>();
// Add game change tracking service and initial indexing service
builder.Services.AddHostedService<GameIndexingBackgroundService>();
builder.Services.AddScoped<GameChangeTrackingService>();

// add igdb import service -- TODO: to be removed later
// get igdb settings from configuration
builder.Services.Configure<IgdbSettings>(
        builder.Configuration.GetSection(IgdbSettings.SectionName));
// Register HttpClient with typed client
// builder.Services.AddHttpClient<IgdbImportService>(client =>
// {
//     client.Timeout = TimeSpan.FromMinutes(5); // Adjust timeout as needed
// });
// builder.Services.AddScoped<IgdbImportService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// add db context for todo example
//builder.Services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));

// add db context for mysql
// TODO: add connection string in cofnigguration
var mySqlConnectionString = builder.Configuration.GetConnectionString("mysqldb");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(mySqlConnectionString, new MySqlServerVersion(new Version())));

// add identity services
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// configure identity options
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
});

// add jwt authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
jwtSettings.SecretKey = builder.Configuration["Jwt:SecretKey"]; // From secrets

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); ;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkew),
        RequireExpirationTime = true,
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = builder.Configuration["AUthentication:Facebook:AppSecret"];
});

// add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowVueApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

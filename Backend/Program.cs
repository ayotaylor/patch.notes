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
using Backend.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Azure.Identity;

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

// Add memory caching for semantic keyword cache
builder.Services.AddMemoryCache();

// Add semantic configuration service as singleton (loaded once at startup)
builder.Services.AddSingleton<ISemanticConfigurationService, SemanticConfigurationService>();


// Add category normalization service as scoped (to work with scoped DbContext)
builder.Services.AddScoped<CategoryNormalizationService>();

// Add hybrid embedding enhancer
builder.Services.AddScoped<HybridEmbeddingEnhancer>();

// Add recommendation services
builder.Services.AddHttpClient<GroqLanguageModel>();

// Check if Qdrant is configured before registering Qdrant-dependent services
var qdrantUrl = builder.Configuration["Qdrant:Url"];
var isQdrantEnabled = !string.IsNullOrEmpty(qdrantUrl);

if (isQdrantEnabled)
{
    builder.Services.AddSingleton<QdrantClient>(provider =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var url = configuration["Qdrant:Url"];
        var uri = new Uri(url);
        // Use gRPC port (6334) instead of HTTP port (6333) C# client uses gRPC interface by default
        var grpcPort = uri.Port == 6333 ? 6334 : uri.Port;

        // Configure optimized gRPC channel for high-throughput operations
        var grpcAddress = $"http://{uri.Host}:{grpcPort}";
        var grpcChannelOptions = new Grpc.Net.Client.GrpcChannelOptions
        {
            MaxReceiveMessageSize = 16 * 1024 * 1024, // 16MB
            MaxSendMessageSize = 16 * 1024 * 1024,    // 16MB
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
                PooledConnectionLifetime = TimeSpan.FromMinutes(30),
                MaxConnectionsPerServer = 10,
                ConnectTimeout = TimeSpan.FromSeconds(30),
                ResponseDrainTimeout = TimeSpan.FromSeconds(10),
                RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8
            }
        };

        var channel = Grpc.Net.Client.GrpcChannel.ForAddress(grpcAddress, grpcChannelOptions);
        var grpcClient = new QdrantGrpcClient(channel);
        return new QdrantClient(grpcClient);
    });

    builder.Services.AddScoped<IVectorDatabase, QdrantVectorDatabase>();
    builder.Services.AddScoped<IEmbeddingService, SentenceTransformerEmbeddingService>();
    builder.Services.AddScoped<ILanguageModel, GroqLanguageModel>();
    builder.Services.AddScoped<UserPreferenceService>();
    builder.Services.AddSingleton<ISemanticKeywordCache, SemanticKeywordCache>();
    builder.Services.AddScoped<GameIndexingService>();
    builder.Services.AddScoped<GameRecommendationService>();
    builder.Services.AddScoped<QueryEnhancementService>();
    builder.Services.AddScoped<EmbeddingDimensionValidator>();
    builder.Services.AddSingleton<ConversationStateService>();
    // Add game change tracking service and initial indexing service
    builder.Services.AddHostedService<GameIndexingBackgroundService>();
    builder.Services.AddHostedService<SemanticCacheWarmupService>();
    builder.Services.AddHostedService<CategoryNormalizationInitializationService>();
    builder.Services.AddScoped<GameChangeTrackingService>();
}
else
{
    // Qdrant is not configured - skip recommendation services
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    logger.LogWarning("Qdrant URL is not configured. Recommendation features will be disabled.");
}

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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization to handle UTC dates properly
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        // Note: System.Text.Json automatically serializes DateTime as ISO 8601 format
        // If DateTime.Kind is UTC, it will include the 'Z' suffix
        // To ensure this, all DateTime values should be stored/created as UTC
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// add db context for todo example
//builder.Services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));

var keyVaultUrl = builder.Configuration["AzureKeyVaultUrl"];

if (!string.IsNullOrEmpty(keyVaultUrl))
{
    // 2. We are in Production! Add Key Vault to the config providers.
    // This will OVERWRITE the "DefaultConnection" from appsettings.Development.json
    // with the "ConnectionStrings--DefaultConnection" secret from the Vault.
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl), 
        new DefaultAzureCredential());
}

// add db context for mysql
var mySqlConnectionString = builder.Configuration.GetConnectionString("mysqldb");
if (string.IsNullOrEmpty(mySqlConnectionString))
{
    throw new InvalidOperationException(
        "MySQL connection string 'mysqldb' is not configured. " +
        "For local development, add it to user secrets. " +
        "For production, ensure 'ConnectionStrings--mysqldb' is set in Azure Key Vault.");
}

builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
{
    options.UseMySql(mySqlConnectionString, new MySqlServerVersion(new Version()), mySqlOptions =>
    {
        mySqlOptions.CommandTimeout(120);
        mySqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
    });
    options.EnableSensitiveDataLogging(false);
    options.EnableServiceProviderCaching();
    options.EnableDetailedErrors(false);
}, poolSize: 64);

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

if (string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    throw new InvalidOperationException(
        "JWT Secret Key is not configured. " +
        "For local development, add 'Jwt:SecretKey' to user secrets. " +
        "For production, ensure 'Jwt--SecretKey' is set in Azure Key Vault.");
}

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
});

// Add external authentication providers only if configured
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
{
    builder.Services.AddAuthentication()
        .AddFacebook(options =>
        {
            options.AppId = facebookAppId;
            options.AppSecret = facebookAppSecret;
        });
}

// add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "http://localhost:3000", "https://www.patchnotes.cool")
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

// Azure Container Apps handles HTTPS at ingress - don't redirect in production
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowVueApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Validate embedding dimensions at startup to prevent runtime errors (only if Qdrant is enabled)
if (isQdrantEnabled)
{
    using (var scope = app.Services.CreateScope())
    {
        var validator = scope.ServiceProvider.GetRequiredService<EmbeddingDimensionValidator>();
        var validationResult = await validator.ValidateSystemDimensionsAsync();

        if (!validationResult)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogCritical("❌ STARTUP FAILED: Embedding dimension validation failed! The system cannot start with incorrect dimensions.");
            logger.LogCritical("Please check your configuration and ensure all embedding dimensions are consistent.");
            logger.LogCritical("Expected dimensions: {ExpectedMessage}", EmbeddingConstants.GetExpectedDimensionsMessage());

            // Terminate the application to prevent runtime errors
            Environment.Exit(1);
        }
    }
}

app.Run();

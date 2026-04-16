using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.Hubs;
using PokerProject.Hubs.GameNotifier;
using PokerProject.Services.Bounties;
using PokerProject.Services.Database;
using PokerProject.Services.Games;
using PokerProject.Services.HallOfFames;
using PokerProject.Services.Players;
using PokerProject.Services.Rounds;
using PokerProject.Services.Scores;
using PokerProject.Services.Users;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

//Using a nugetpackage to load .env file when testing in local
DotNetEnv.Env.Load();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// DATABASE 
//INCOMMENT FOR LOCAL DB
builder.Services.AddDbContext<PokerDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//INCOMMENT FOR PROD DB
//Connection string for online database, loaded from env variable
//var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
//builder.Services.AddDbContext<PokerDbContext>(options => options.UseSqlServer(connectionString));

//Dependency Injection for services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHallOfFameService, HallOfFameService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IBountyService, BountyService>();
builder.Services.AddScoped<IRoundService, RoundService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IGameNotifier, GameNotifier>();

//CORS
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
            .AllowCredentials();
    });
});


//Convert enums to string instead of int
builder.Services
    .AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

//SignalR
builder.Services
    .AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.Name = "PokerAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/gamehub"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/gamehub"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapHub<GameHub>("gamehub");

app.MapControllers();

app.Run();
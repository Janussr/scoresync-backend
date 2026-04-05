using Microsoft.AspNetCore.Authentication.Cookies;
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

var builder = WebApplication.CreateBuilder(args);

//Using a nugetpackage to load .env file when testing in local
DotNetEnv.Env.Load();


// Add services to the container.
builder.Services.AddControllers();
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
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IHallOfFameService, HallOfFameService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IBountyService, BountyService>();
builder.Services.AddScoped<IRoundService, RoundService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IGameNotifier, GameNotifier>();

//CORS
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

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
builder.Services.AddSignalR();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.Name = "PokerAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>("gamehub");

app.MapControllers();

app.Run();
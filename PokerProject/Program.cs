using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PokerProject.Data;
using PokerProject.Hubs;
using PokerProject.Services.Bounties;
using PokerProject.Services.Games;
using PokerProject.Services.HallOfFames;
using PokerProject.Services.Players;
using PokerProject.Services.Rounds;
using PokerProject.Services.Scores;
using PokerProject.Services.Users;
//using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//Using a nugetpackage to load .env file when testing in local
DotNetEnv.Env.Load();

//Connection string for database, loaded from env variable
//var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

//var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
//                ?? throw new InvalidOperationException("JWT_SECRET not set");
//var key = Encoding.ASCII.GetBytes(jwtSecret);

// JWT Authentication
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(key),
//        ValidateIssuer = false,
//        ValidateAudience = false,
//        ClockSkew = TimeSpan.Zero,
//    };
//});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// DATABASE 

//incomment for local db
builder.Services.AddDbContext<PokerDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//incomment for prod db
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


app.MapGet("/", () => "Backend runs!");

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
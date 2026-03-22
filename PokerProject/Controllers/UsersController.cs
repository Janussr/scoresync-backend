using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokerProject.DTOs;
using PokerProject.Models;
using PokerProject.Services.Users;
using System.Data;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterUserDto dto)
    {
        try
        {
            var user = await _userService.RegisterAsync(dto);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }
        catch (ArgumentException ex) 
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });
            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserDto dto)
    {
        var user = await _userService.ValidateUserAsync(dto.Username, dto.Password);
        if (user == null)
            return Unauthorized(new { message = "Invalid username or password" });

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role.ToString())
    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // husk login mellem sessions TODO true or false?
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        return Ok(new { message = "Logged in" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("PokerAuth"); 
        return Ok(new { message = "Logged out" });
    }

    // api/users/me
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _userService.GetUserByIdAsync(int.Parse(userIdClaim));
        if (user == null) return NotFound();

        return Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("admin/reset-password")]
    public async Task<IActionResult> AdminResetPassword(AdminResetPasswordDto dto)
    {
        try
        {
            var user = await _userService.AdminResetPasswordAsync(dto.UserId, dto.NewPassword);

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "Password reset successfully", user });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("admin/set-role")]
    public async Task<IActionResult> SetRole(SetUserRoleDto dto)
    {
        var user = await _userService.SetUserRoleAsync(dto);

        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(new
        {
            message = $"User role updated to {dto.Role}",
            user
        });
    }
}
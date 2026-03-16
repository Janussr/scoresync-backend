using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokerProject.DTOs;
using PokerProject.Models;
using PokerProject.Services.Users;

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
        catch (ArgumentException ex) // fx username already exists
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected server error" });
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
            return StatusCode(500, new { message = "Unexpected server error" });
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
            return StatusCode(500, new { message = "Unexpected server error" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<object>> Login(LoginUserDto dto)
    {
        try
        {
            var token = await _userService.LoginAndGenerateTokenAsync(dto.Username, dto.Password);
            if (token == null)
                return Unauthorized(new { message = "Invalid username or password" });

            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected server error" });
        }
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
            return StatusCode(500, new { message = "Unexpected server error" });
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
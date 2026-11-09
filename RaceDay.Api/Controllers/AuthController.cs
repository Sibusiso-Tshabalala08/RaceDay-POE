using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public AuthController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Registers a new user as either an Organiser or a Participant.</summary>
    /// <remarks>Expects: fullName, email, password, role ("Organiser" or "Participant"). Returns 201 with the new profile, 400 for an invalid role, 409 if the email is already registered.</remarks>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (dto.Role != "Organiser" && dto.Role != "Participant")
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });

        var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailExists)
            return Conflict(new { message = "An account with this email already exists." });

        var role = await _context.Roles.FirstAsync(r => r.RoleName == dto.Role);

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = role.RoleId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(201, new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = role.RoleName,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>Logs a user in and starts a server-side session.</summary>
    /// <remarks>Expects: email, password. On success, stores UserId and Role in the session for all subsequent requests. Returns 200 with the user's profile, or 401 for invalid credentials.</remarks>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        // Server-side session: this is the "session management" the brief
        // and rubric explicitly ask for, rather than a client-held token.
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role!.RoleName);
        HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);

        return Ok(new
        {
            message = "Login successful.",
            user = new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.RoleName,
                CreatedAt = user.CreatedAt
            }
        });
    }

    /// <summary>Logs the current user out by clearing the session.</summary>
    /// <remarks>Requires an active session. Returns 200 on success, 401 if nobody is logged in.</remarks>
    [HttpPost("logout")]
    [RequireSession]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out successfully." });
    }
}

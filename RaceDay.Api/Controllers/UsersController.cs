using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public UsersController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    /// <remarks>Requires an active session (Organiser or Participant). Returns 200 with the profile, or 401 if nobody is logged in.</remarks>
    [HttpGet("me")]
    [RequireSession]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = HttpContext.GetUserId();
        var user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.UserId == userId);

        return Ok(new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role!.RoleName,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>Updates the logged-in user's own profile (full name and email).</summary>
    /// <remarks>Requires an active session. Returns 200 with the updated profile, or 400 if the new email is already taken by another account.</remarks>
    [HttpPut("me")]
    [RequireSession]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
    {
        var userId = HttpContext.GetUserId();
        var user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.UserId == userId);

        var emailTaken = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != userId);
        if (emailTaken)
            return BadRequest(new { message = "That email is already in use." });

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        await _context.SaveChangesAsync();

        return Ok(new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role!.RoleName,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>Retrieves a specific user's profile by ID.</summary>
    /// <remarks>Organiser only — e.g. to check a participant's details. Returns 200 with the profile, 403 if the caller isn't an Organiser, or 404 if the user doesn't exist.</remarks>
    [HttpGet("{id}")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null)
            return NotFound(new { message = "User not found." });

        return Ok(new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role!.RoleName,
            CreatedAt = user.CreatedAt
        });
    }
}

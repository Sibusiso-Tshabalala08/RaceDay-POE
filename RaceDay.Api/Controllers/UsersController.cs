using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public UsersController(RaceDayDbContext context, IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    private static UserProfileDto ToProfile(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role!.RoleName,
        CreatedAt = user.CreatedAt,
        ProfilePictureUrl = user.ProfilePictureUrl
    };

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    /// <remarks>Requires an active session (Organiser or Participant). Returns 200 with the profile, or 401 if nobody is logged in.</remarks>
    [HttpGet("me")]
    [RequireSession]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = HttpContext.GetUserId();
        var user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.UserId == userId);

        return Ok(ToProfile(user));
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

        return Ok(ToProfile(user));
    }

    /// <summary>Uploads (or replaces) the logged-in user's profile picture, storing it in Azure Blob Storage.</summary>
    /// <remarks>Requires an active session. Send multipart/form-data with one image file in the "file" field (JPG, PNG, WEBP or GIF, max 5 MB). The blob URL is saved on the user. Returns 200 with the updated profile, 400 for a missing or invalid image, 502 if Azure is unavailable, or 503 if Azure Blob Storage is not configured.</remarks>
    [HttpPost("me/picture")]
    [RequireSession]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMyPicture(IFormFile file, CancellationToken cancellationToken)
    {
        var check = await ImageRules.ValidateAsync(file);
        if (!check.Ok)
            return BadRequest(new { message = check.Error });

        var userId = HttpContext.GetUserId();
        var user = await _context.Users.Include(u => u.Role)
            .FirstAsync(u => u.UserId == userId, cancellationToken);

        try
        {
            await using var stream = file.OpenReadStream();
            user.ProfilePictureUrl = await _blobStorage.UploadImageAsync(
                stream, file.ContentType, check.Extension, "profile-pictures", cancellationToken);
        }
        catch (BlobStorageNotConfiguredException)
        {
            return StatusCode(503, new { message = "Image storage is not configured on the server." });
        }
        catch (BlobStorageUnavailableException)
        {
            return StatusCode(502, new { message = "Image storage is currently unavailable. Please try again later." });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ToProfile(user));
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

        return Ok(ToProfile(user));
    }
}

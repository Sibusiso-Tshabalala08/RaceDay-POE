using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private static EnrolmentResponseDto ToDto(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant?.FullName ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.CategoryName ?? string.Empty,
        EventId = e.Category?.EventId ?? 0,
        EventName = e.Category?.Event?.EventName ?? string.Empty,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };

    /// <summary>Enrols the logged-in Participant into a specific category.</summary>
    /// <remarks>Participant only. Returns 201 with the new enrolment, 400 if the category is full, 404 if the category doesn't exist, or 409 if already enrolled in this category.</remarks>
    [HttpPost("api/categories/{categoryId}/enrolments")]
    [RequireSession("Participant")]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await _context.Categories
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category is null)
            return NotFound(new { message = "Category not found." });

        var activeCount = category.Enrolments.Count(e => e.Status == "Confirmed");
        if (activeCount >= category.MaxParticipants)
            return BadRequest(new { message = "This category is full." });

        var participantId = HttpContext.GetUserId();

        var alreadyEnrolled = category.Enrolments.Any(e => e.ParticipantId == participantId && e.Status == "Confirmed");
        if (alreadyEnrolled)
            return Conflict(new { message = "You are already enrolled in this category." });

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            CategoryId = categoryId,
            Status = "Confirmed"
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        await _context.Entry(enrolment).Reference(e => e.Participant).LoadAsync();
        await _context.Entry(enrolment).Reference(e => e.Category).LoadAsync();
        await _context.Entry(enrolment.Category!).Reference(c => c.Event).LoadAsync();

        return StatusCode(201, ToDto(enrolment));
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <remarks>Participant only. Returns 200 with an array of enrolments.</remarks>
    [HttpGet("api/users/me/enrolments")]
    [RequireSession("Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = HttpContext.GetUserId();

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .Where(e => e.ParticipantId == participantId)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>Lists all enrolments for an event owned by the logged-in Organiser.</summary>
    /// <remarks>Organiser only, and only for events they created. Returns 200 with an array of enrolments, 403 if the caller doesn't own the event, or 404 if it doesn't exist.</remarks>
    [HttpGet("api/events/{eventId}/enrolments")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only view enrolments for events you created." });

        var enrolments = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .Where(e => e.Category!.EventId == eventId)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <remarks>Participant only, and only for their own enrolment. Returns 204 on success, 403 if it isn't theirs, or 404 if it doesn't exist.</remarks>
    [HttpDelete("api/enrolments/{id}")]
    [RequireSession("Participant")]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _context.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.ParticipantId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only cancel your own enrolments." });

        enrolment.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

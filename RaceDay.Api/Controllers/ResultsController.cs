using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private static readonly string[] ValidStatuses = { "Finished", "DNF", "DNS", "DQ" };

    private static ResultResponseDto ToDto(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantName = r.Enrolment?.Participant?.FullName ?? string.Empty,
        CategoryName = r.Enrolment?.Category?.CategoryName ?? string.Empty,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status
    };

    /// <summary>Captures a result (finish time, position, status) for a specific enrolment.</summary>
    /// <remarks>Organiser only, and only for events they created. Expects: finishTime, position, status (Finished/DNF/DNS/DQ). Returns 201 with the new result, 400 for an invalid status, 403 if the caller doesn't own the event, 404 if the enrolment doesn't exist, or 409 if a result already exists for it.</remarks>
    [HttpPost("api/enrolments/{enrolmentId}/results")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> CreateResult(int enrolmentId, ResultCreateDto dto)
    {
        if (!ValidStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Status must be one of: Finished, DNF, DNS, DQ." });

        var enrolment = await _context.Enrolments
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Category!.Event!.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only capture results for events you created." });

        if (enrolment.Result is not null)
            return Conflict(new { message = "A result has already been recorded for this enrolment." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position,
            Status = dto.Status
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        await _context.Entry(result).Reference(r => r.Enrolment).LoadAsync();
        await _context.Entry(result.Enrolment!).Reference(e => e.Participant).LoadAsync();
        await _context.Entry(result.Enrolment!).Reference(e => e.Category).LoadAsync();

        return StatusCode(201, ToDto(result));
    }

    /// <summary>Updates a previously captured result (e.g. to correct a timing error).</summary>
    /// <remarks>Organiser only, and only for events they created. Returns 200 with the updated result, 400 for an invalid status, 403 if the caller doesn't own the event, or 404 if the result doesn't exist.</remarks>
    [HttpPut("api/results/{id}")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> UpdateResult(int id, ResultCreateDto dto)
    {
        if (!ValidStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Status must be one of: Finished, DNF, DNS, DQ." });

        var result = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result is null)
            return NotFound(new { message = "Result not found." });

        if (result.Enrolment!.Category!.Event!.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only update results for events you created." });

        result.FinishTime = dto.FinishTime;
        result.Position = dto.Position;
        result.Status = dto.Status;

        await _context.SaveChangesAsync();
        return Ok(ToDto(result));
    }

    /// <summary>Returns the logged-in Participant's personal performance history across all events.</summary>
    /// <remarks>Participant only. Returns 200 with an array of results.</remarks>
    [HttpGet("api/users/me/results")]
    [RequireSession("Participant")]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = HttpContext.GetUserId();

        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .ToListAsync();

        return Ok(results.Select(ToDto));
    }

    /// <summary>Returns the full results/leaderboard for an event.</summary>
    /// <remarks>No login required. Returns 200 with an array of results, or 404 if the event doesn't exist.</remarks>
    [HttpGet("api/events/{eventId}/results")]
    public async Task<IActionResult> GetResultsForEvent(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
            return NotFound(new { message = "Event not found." });

        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Where(r => r.Enrolment!.Category!.EventId == eventId)
            .ToListAsync();

        return Ok(results.Select(ToDto));
    }
}

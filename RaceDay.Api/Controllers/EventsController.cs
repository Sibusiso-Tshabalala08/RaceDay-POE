using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IWeatherService _weatherService;
    private readonly IBlobStorageService _blobStorage;

    public EventsController(RaceDayDbContext context, IWeatherService weatherService, IBlobStorageService blobStorage)
    {
        _context = context;
        _weatherService = weatherService;
        _blobStorage = blobStorage;
    }

    private static EventResponseDto ToDto(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Location = e.Location,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        Distance = e.Distance,
        EventType = e.EventType,
        RouteMapUrl = e.RouteMapUrl,
        BannerImageUrl = e.BannerImageUrl,
        Description = e.Description
    };

    private static EventSummaryDto ToSummaryDto(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Location = e.Location,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        Distance = e.Distance,
        EventType = e.EventType,
        RouteMapUrl = e.RouteMapUrl,
        BannerImageUrl = e.BannerImageUrl,
        Description = e.Description,
        CategoryCount = e.Categories.Count,
        EnrolmentCount = e.Categories.Sum(c => c.Enrolments.Count(en => en.Status != "Cancelled"))
    };

    /// <summary>Lists all events, for public browsing.</summary>
    /// <remarks>No login required. Returns 200 with an array of events.</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        var events = await _context.Events.Include(e => e.Organiser).ToListAsync();
        return Ok(events.Select(ToDto));
    }

    /// <summary>Lists the events created by the logged-in Organiser, with enrolment counts (for the Organiser dashboard).</summary>
    /// <remarks>Organiser only. Returns 200 with an array of events ordered by date, each with enrolmentCount and categoryCount.</remarks>
    [HttpGet("mine")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> GetMyEvents()
    {
        var organiserId = HttpContext.GetUserId();

        var events = await _context.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToSummaryDto));
    }

    /// <summary>Retrieves full details for a single event.</summary>
    /// <remarks>No login required. Returns 200 with event details, or 404 if it doesn't exist.</remarks>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEventById(int id)
    {
        var ev = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        return Ok(ToDto(ev));
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>Organiser only. Expects: eventName, eventDate, location, latitude, longitude, distance, eventType (Run/Walk/Cycle), description. Returns 201 with the new event, or 400 for an invalid event type.</remarks>
    [HttpPost]
    [RequireSession("Organiser")]
    public async Task<IActionResult> CreateEvent(EventCreateDto dto)
    {
        if (!IsValidEventType(dto.EventType))
            return BadRequest(new { message = "EventType must be 'Run', 'Walk', or 'Cycle'." });

        var organiserId = HttpContext.GetUserId();

        var ev = new Event
        {
            OrganiserId = organiserId,
            EventName = dto.EventName,
            EventDate = dto.EventDate,
            Location = dto.Location,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Distance = dto.Distance,
            EventType = dto.EventType,
            Description = dto.Description
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        await _context.Entry(ev).Reference(e => e.Organiser).LoadAsync();
        return StatusCode(201, ToDto(ev));
    }

    /// <summary>Updates an event owned by the logged-in Organiser.</summary>
    /// <remarks>Organiser only, and only for events they created. Returns 200 with the updated event, 403 if the caller doesn't own the event, or 404 if it doesn't exist.</remarks>
    [HttpPut("{id}")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, EventCreateDto dto)
    {
        if (!IsValidEventType(dto.EventType))
            return BadRequest(new { message = "EventType must be 'Run', 'Walk', or 'Cycle'." });

        var ev = await _context.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only edit events you created." });

        ev.EventName = dto.EventName;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Latitude = dto.Latitude;
        ev.Longitude = dto.Longitude;
        ev.Distance = dto.Distance;
        ev.EventType = dto.EventType;
        ev.Description = dto.Description;

        await _context.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>Uploads (or replaces) the banner image of an event, storing it in Azure Blob Storage.</summary>
    /// <remarks>Organiser only, and only for events they created. Send multipart/form-data with one image file in the "file" field (JPG, PNG, WEBP or GIF, max 5 MB). The blob URL is saved on the event. Returns 200 with the updated event, 400 for a missing or invalid image, 403 if the caller doesn't own the event, 404 if it doesn't exist, 502 if Azure is unavailable, or 503 if Azure Blob Storage is not configured.</remarks>
    [HttpPost("{id}/banner")]
    [RequireSession("Organiser")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken cancellationToken)
    {
        var ev = await _context.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id, cancellationToken);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only upload banners for events you created." });

        var check = await ImageRules.ValidateAsync(file);
        if (!check.Ok)
            return BadRequest(new { message = check.Error });

        try
        {
            await using var stream = file.OpenReadStream();
            ev.BannerImageUrl = await _blobStorage.UploadImageAsync(
                stream, file.ContentType, check.Extension, "event-banners", cancellationToken);
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
        return Ok(ToDto(ev));
    }

    /// <summary>Deletes an event owned by the logged-in Organiser.</summary>
    /// <remarks>Organiser only, and only for events they created. Returns 204 on success, 403 if the caller doesn't own the event, or 404 if it doesn't exist.</remarks>
    [HttpDelete("{id}")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only delete events you created." });

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Fetches a live weather forecast for the event's location, using its stored latitude/longitude.</summary>
    /// <remarks>No login required. Returns 200 with forecast data, 400 if the event has no coordinates set, 404 if the event doesn't exist, or 502 if the weather provider is unreachable.</remarks>
    [HttpGet("{id}/weather")]
    public async Task<IActionResult> GetEventWeather(int id)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.Latitude is null || ev.Longitude is null)
            return BadRequest(new { message = "This event has no location coordinates set." });

        var forecast = await _weatherService.GetForecastAsync(ev.Latitude.Value, ev.Longitude.Value);
        if (forecast is null)
            return StatusCode(502, new { message = "Weather provider is currently unavailable." });

        return Ok(forecast);
    }

    private static bool IsValidEventType(string eventType) =>
        eventType is "Run" or "Walk" or "Cycle";
}

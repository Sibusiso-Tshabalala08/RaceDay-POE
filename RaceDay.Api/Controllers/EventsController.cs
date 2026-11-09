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

    public EventsController(RaceDayDbContext context, IWeatherService weatherService)
    {
        _context = context;
        _weatherService = weatherService;
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
        Description = e.Description
    };

    /// <summary>Lists all events, for public browsing.</summary>
    /// <remarks>No login required. Returns 200 with an array of events.</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        var events = await _context.Events.Include(e => e.Organiser).ToListAsync();
        return Ok(events.Select(ToDto));
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

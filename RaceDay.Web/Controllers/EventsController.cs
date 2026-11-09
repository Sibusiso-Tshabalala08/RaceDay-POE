using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant browsing: the home page of upcoming events, and an event's detail page.</summary>
[RequireLogin]
public class EventsController : RaceDayController
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api) => _api = api;

    // GET /Events?search=&type=&distance=&showPast=
    [RequireLogin(Roles.Participant)]
    public async Task<IActionResult> Index(string? search, string? type, string? distance, bool showPast = false)
    {
        var result = await _api.GetEventsAsync();
        if (FailPage(result) is { } failed) return failed;

        var all = result.Data ?? new List<EventDto>();
        var upcoming = all.Where(e => e.EventDate >= DateTime.Today).ToList();

        // Filters only narrow what is displayed - the data itself comes from the API.
        IEnumerable<EventDto> filtered = showPast ? all : upcoming;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            filtered = filtered.Where(e =>
                e.EventName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(type))
            filtered = filtered.Where(e => string.Equals(e.EventType, type, StringComparison.OrdinalIgnoreCase));

        filtered = distance switch
        {
            "short" => filtered.Where(e => e.Distance is not null && e.Distance <= 10m),
            "medium" => filtered.Where(e => e.Distance is > 10m and <= 21.1m),
            "long" => filtered.Where(e => e.Distance is > 21.1m),
            _ => filtered
        };

        return View(new EventsIndexViewModel
        {
            Events = (showPast ? filtered.OrderByDescending(e => e.EventDate) : filtered.OrderBy(e => e.EventDate)).ToList(),
            Search = search,
            Type = type,
            Distance = distance,
            ShowPast = showPast,
            TotalUpcoming = upcoming.Count
        });
    }

    // GET /Events/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (FailPage(ev) is { } failedEvent) return failedEvent;

        var categories = await _api.GetCategoriesAsync(id);
        if (FailPage(categories) is { } failedCats) return failedCats;

        var model = new EventDetailViewModel
        {
            Event = ev.Data!,
            Categories = categories.Data ?? new List<CategoryDto>(),
            Weather = ev.Data!.Latitude is not null ? await _api.GetWeatherAsync(id) : null
        };

        // Which categories has this participant already joined? (so the button can say "Enrolled")
        if (CurrentUser!.IsParticipant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            if (mine.Success && mine.Data is not null)
                model.EnrolledCategoryIds = mine.Data
                    .Where(e => e.EventId == id && e.Status != "Cancelled")
                    .Select(e => e.CategoryId).ToHashSet();
        }

        return View(model);
    }
}

using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: create, edit and delete their events (including the banner image).</summary>
[RequireLogin(Roles.Organiser)]
[Route("Organiser/Events")]
public class ManageEventsController : RaceDayController
{
    private const long MaxBannerBytes = 5 * 1024 * 1024;
    private readonly ApiClient _api;

    public ManageEventsController(ApiClient api) => _api = api;

    // GET /Organiser/Events
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEventsAsync();
        if (FailPage(result) is { } failed) return failed;

        return View(result.Data!.OrderBy(e => e.EventDate).ToList());
    }

    // GET /Organiser/Events/Create
    [HttpGet("Create")]
    public IActionResult Create() =>
        View("Form", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(7) });

    // POST /Organiser/Events/Create
    [HttpPost("Create")]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateBanner(model);
        if (!ModelState.IsValid) return View("Form", model);

        var created = await _api.CreateEventAsync(ToRequest(model));
        if (created.Unauthorized) return SessionExpired();
        if (!created.Success)
        {
            AddApiErrors(created);
            return View("Form", model);
        }

        // The event now exists, so a banner problem must not lose it.
        var bannerNote = await TryUploadBannerAsync(created.Data!.EventId, model.Banner);
        if (bannerNote is null)
            FlashSuccess($"“{created.Data.EventName}” has been created. Add categories so people can enrol.");
        else
            FlashError($"“{created.Data.EventName}” was created, but the banner could not be uploaded: {bannerNote}");

        return RedirectToAction("Index", "ManageCategories", new { eventId = created.Data.EventId });
    }

    // GET /Organiser/Events/Edit/5
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (FailPage(result) is { } failed) return failed;
        if (result.Data!.OrganiserId != CurrentUser!.UserId)
            return RedirectToAction("AccessDenied", "Account");

        return View("Form", ToForm(result.Data));
    }

    // POST /Organiser/Events/Edit/5
    [HttpPost("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        ValidateBanner(model);
        if (!ModelState.IsValid) return View("Form", model);

        var updated = await _api.UpdateEventAsync(id, ToRequest(model));
        if (updated.Unauthorized) return SessionExpired();
        if (updated.Forbidden) return RedirectToAction("AccessDenied", "Account");
        if (!updated.Success)
        {
            AddApiErrors(updated);
            return View("Form", model);
        }

        var bannerNote = await TryUploadBannerAsync(id, model.Banner);
        if (bannerNote is null)
            FlashSuccess("Event updated.");
        else
            FlashError($"Event details were saved, but the banner could not be uploaded: {bannerNote}");

        return RedirectToAction(nameof(Index));
    }

    // POST /Organiser/Events/Delete/5
    [HttpPost("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteEventAsync(id);
        if (FailAction(result) is { } expired) return expired;
        if (result.Success) FlashSuccess("Event deleted.");
        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------
    private void ValidateBanner(EventFormViewModel model)
    {
        if (model.Banner is null || model.Banner.Length == 0) return;
        if (model.Banner.Length > MaxBannerBytes)
            ModelState.AddModelError(nameof(model.Banner), "That image is too big. Please choose one under 5 MB.");
    }

    /// <summary>Returns null on success (or when no file was chosen), otherwise a user-friendly reason.</summary>
    private async Task<string?> TryUploadBannerAsync(int eventId, IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        var result = await _api.UploadBannerAsync(eventId, file);
        return result.Success ? null : result.FullMessage;
    }

    private static EventRequest ToRequest(EventFormViewModel m) => new()
    {
        EventName = m.EventName.Trim(),
        EventDate = m.EventDate!.Value,
        Location = m.Location.Trim(),
        Distance = m.Distance,
        EventType = m.EventType,
        Description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim(),
        Latitude = m.Latitude,
        Longitude = m.Longitude
    };

    private static EventFormViewModel ToForm(EventDto e) => new()
    {
        EventId = e.EventId,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        Description = e.Description,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        ExistingBannerUrl = e.BannerImageUrl
    };
}

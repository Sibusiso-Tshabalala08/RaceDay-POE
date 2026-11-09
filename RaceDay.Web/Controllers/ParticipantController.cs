using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Everything a Participant does after finding an event: enrol, track enrolments, see results.</summary>
[RequireLogin(Roles.Participant)]
public class ParticipantController : RaceDayController
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api) => _api = api;

    // POST /Participant/Enrol
    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            FlashError("Please choose a category to enrol in.");
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var result = await _api.EnrolAsync(categoryId);
        if (result.Unauthorized) return SessionExpired();

        if (result.Success)
            FlashSuccess($"You're in! Enrolment confirmed for {result.Data?.EventName ?? "the event"} ({result.Data?.CategoryName}).");
        else
            FlashError(result.Conflict
                ? (string.IsNullOrWhiteSpace(result.Message) ? "You can't enrol in this category." : result.Message)
                : result.FullMessage);

        return result.Success
            ? RedirectToAction(nameof(MyEvents))
            : RedirectToAction("Details", "Events", new { id = eventId });
    }

    // GET /Participant/MyEvents
    public async Task<IActionResult> MyEvents()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        if (FailPage(result) is { } failed) return failed;

        var all = result.Data ?? new List<EnrolmentDto>();
        var today = DateTime.Today;

        return View(new MyEventsViewModel
        {
            Upcoming = all.Where(e => e.EventDate is null || e.EventDate >= today)
                          .OrderBy(e => e.EventDate).ToList(),
            Past = all.Where(e => e.EventDate < today)
                      .OrderByDescending(e => e.EventDate).ToList()
        });
    }

    // POST /Participant/Cancel
    [HttpPost]
    public async Task<IActionResult> Cancel(int enrolmentId)
    {
        var result = await _api.CancelEnrolmentAsync(enrolmentId);
        if (FailAction(result) is { } expired) return expired;

        if (result.Success) FlashSuccess("Your enrolment has been cancelled.");
        return RedirectToAction(nameof(MyEvents));
    }

    // GET /Participant/MyResults
    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetMyResultsAsync();
        if (FailPage(result) is { } failed) return failed;

        return View(result.Data ?? new List<ResultDto>());
    }
}

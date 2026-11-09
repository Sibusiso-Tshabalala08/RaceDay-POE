using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: who has enrolled in an event, in which category, and with what status.</summary>
[RequireLogin(Roles.Organiser)]
[Route("Organiser/Enrolments")]
public class ManageEnrolmentsController : RaceDayController
{
    private readonly ApiClient _api;

    public ManageEnrolmentsController(ApiClient api) => _api = api;

    // GET /Organiser/Enrolments?eventId=5&status=Confirmed
    [HttpGet("")]
    public async Task<IActionResult> Index(int? eventId, string? status)
    {
        var events = await _api.GetMyEventsAsync();
        if (FailPage(events) is { } failed) return failed;

        var list = events.Data!.OrderBy(e => e.EventDate).ToList();
        var selected = list.FirstOrDefault(e => e.EventId == eventId)
                       ?? list.FirstOrDefault(e => e.EventDate >= DateTime.Today)
                       ?? list.FirstOrDefault();

        var model = new ManageEnrolmentsViewModel { Events = list, Selected = selected, StatusFilter = status };

        if (selected is not null)
        {
            var enrolments = await _api.GetEventEnrolmentsAsync(selected.EventId);
            if (FailPage(enrolments) is { } failedEnrolments) return failedEnrolments;

            model.Enrolments = (enrolments.Data ?? new List<EnrolmentDto>())
                .Where(e => string.IsNullOrWhiteSpace(status) || e.Status == status)
                .OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName)
                .ToList();
        }

        return View(model);
    }
}

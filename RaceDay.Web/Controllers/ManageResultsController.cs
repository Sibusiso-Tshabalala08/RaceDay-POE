using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: record each participant's finish time, position and status.</summary>
[RequireLogin(Roles.Organiser)]
[Route("Organiser/Results")]
public class ManageResultsController : RaceDayController
{
    private readonly ApiClient _api;

    public ManageResultsController(ApiClient api) => _api = api;

    // GET /Organiser/Results?eventId=5
    [HttpGet("")]
    public async Task<IActionResult> Index(int? eventId)
    {
        var events = await _api.GetMyEventsAsync();
        if (FailPage(events) is { } failed) return failed;

        var list = events.Data!.OrderByDescending(e => e.EventDate).ToList();
        var selected = list.FirstOrDefault(e => e.EventId == eventId)
                       ?? list.FirstOrDefault(e => e.EventDate <= DateTime.Today)
                       ?? list.FirstOrDefault();

        var model = new ManageResultsViewModel { Events = list, Selected = selected };

        if (selected is not null)
        {
            var enrolments = await _api.GetEventEnrolmentsAsync(selected.EventId);
            if (FailPage(enrolments) is { } failedEnrolments) return failedEnrolments;

            var results = await _api.GetEventResultsAsync(selected.EventId);
            if (FailPage(results) is { } failedResults) return failedResults;

            var byEnrolment = (results.Data ?? new List<ResultDto>()).ToDictionary(r => r.EnrolmentId);

            model.Rows = (enrolments.Data ?? new List<EnrolmentDto>())
                .Where(e => e.Status != "Cancelled")
                .OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName)
                .Select(e => new ResultRow
                {
                    Enrolment = e,
                    Result = byEnrolment.GetValueOrDefault(e.EnrolmentId)
                }).ToList();
        }

        return View(model);
    }

    // POST /Organiser/Results/Save   (creates the result, or updates it when ResultId > 0)
    [HttpPost("Save")]
    public async Task<IActionResult> Save(ResultFormViewModel form)
    {
        var back = RedirectToAction(nameof(Index), new { eventId = form.EventId });

        if (!ModelState.IsValid)
        {
            FlashError(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return back;
        }

        string? finishTime = null;
        if (!string.IsNullOrWhiteSpace(form.FinishTime))
        {
            if (!TimeSpan.TryParse(form.FinishTime, System.Globalization.CultureInfo.InvariantCulture, out var ts) || ts < TimeSpan.Zero)
            {
                FlashError("Finish time must look like 01:45:30 (hours:minutes:seconds).");
                return back;
            }
            finishTime = ts.ToString("c", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (form.Status == "Finished" && finishTime is null)
        {
            FlashError("A finisher needs a finish time. Choose DNF, DNS or DQ if they didn't finish.");
            return back;
        }

        var request = new ResultRequest
        {
            FinishTime = form.Status == "Finished" ? finishTime : null,
            Position = form.Status == "Finished" ? form.Position : null,
            Status = form.Status
        };

        var result = form.ResultId > 0
            ? await _api.UpdateResultAsync(form.ResultId, request)
            : await _api.CreateResultAsync(form.EnrolmentId, request);

        if (FailAction(result) is { } expired) return expired;
        if (result.Success) FlashSuccess("Result saved.");
        return back;
    }
}

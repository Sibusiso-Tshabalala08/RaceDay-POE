using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RequireLogin(Roles.Organiser)]
public class DashboardController : RaceDayController
{
    private readonly ApiClient _api;

    public DashboardController(ApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEventsAsync();
        if (FailPage(result) is { } failed) return failed;

        return View(new DashboardViewModel { Events = result.Data ?? new List<EventDto>() });
    }
}

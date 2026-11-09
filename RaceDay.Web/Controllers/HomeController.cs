using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : RaceDayController
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api) => _api = api;

    /// <summary>Public landing page. Logged-in users go straight to their own home.</summary>
    public async Task<IActionResult> Index()
    {
        if (CurrentUser is { } user) return RedirectToHomeFor(user);

        // A few upcoming events as a teaser; if the API is down the page still loads.
        var events = await _api.GetEventsAsync();
        var teaser = events.Success && events.Data is not null
            ? events.Data.Where(e => e.EventDate >= DateTime.Today).OrderBy(e => e.EventDate).Take(3).ToList()
            : new List<EventDto>();
        ViewBag.ApiDown = events.Unavailable;
        return View(teaser);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        Response.StatusCode = 500;
        // The exception is logged by the framework; the visitor only sees a friendly page.
        return View(new ErrorViewModel
        {
            Title = "Something went wrong on our side",
            Message = "We've hit an unexpected problem. Please try again - if it keeps happening, tell the organiser. (Reference: " + requestId + ")",
            StatusCode = 500
        });
    }

    [IgnoreAntiforgeryToken]
    public IActionResult StatusPage(int code)
    {
        var (title, message) = code switch
        {
            404 => ("Page not found", "We couldn't find that page. It may have moved, or the link may be wrong."),
            403 => ("Access denied", "You don't have permission to see that page."),
            400 => ("That request didn't look right", "Please go back and try again."),
            _ => ("Something went wrong", "Please try again.")
        };
        Response.StatusCode = code;
        return View("Error", new ErrorViewModel { Title = title, Message = message, StatusCode = code });
    }
}

using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Base class for every controller: shortcuts for the current user, flash
/// messages, and one place that turns a failed API call into the right
/// friendly response.
/// </summary>
public abstract class RaceDayController : Controller
{
    protected SessionUser? CurrentUser => HttpContext.Session.GetUser();

    protected void FlashSuccess(string message) => TempData["Success"] = message;
    protected void FlashError(string message) => TempData["Error"] = message;
    protected void FlashInfo(string message) => TempData["Info"] = message;

    /// <summary>
    /// The API said 401: its session (30 minutes idle) has ended, so the MVC
    /// session is cleared and the user is sent to log in again.
    /// </summary>
    protected IActionResult SessionExpired()
    {
        HttpContext.Session.Clear();
        FlashError("Your session has expired. Please log in again.");
        return RedirectToAction("Login", "Account");
    }

    /// <summary>
    /// For GET pages: if the API call failed, returns the page to show instead
    /// (login redirect, access denied, or a friendly error). Null means success.
    /// </summary>
    protected IActionResult? FailPage<T>(ApiResult<T> result)
    {
        if (result.Success) return null;
        if (result.Unauthorized) return SessionExpired();
        if (result.Forbidden) return RedirectToAction("AccessDenied", "Account");

        Response.StatusCode = result.Unavailable ? 503 : (int)result.StatusCode;
        return View("~/Views/Shared/ApiError.cshtml", new ErrorViewModel
        {
            Title = result.Unavailable ? "We can't reach RaceDay right now"
                  : result.NotFound ? "Not found"
                  : "Something went wrong",
            Message = result.FullMessage,
            ApiUnavailable = result.Unavailable,
            StatusCode = (int)result.StatusCode
        });
    }

    /// <summary>For POST actions: flash the failure; returns the session-expired redirect when that is the cause.</summary>
    protected IActionResult? FailAction<T>(ApiResult<T> result)
    {
        if (result.Success) return null;
        if (result.Unauthorized) return SessionExpired();
        FlashError(result.FullMessage);
        return null;
    }

    /// <summary>Copies API validation messages onto the form so they show under the fields.</summary>
    protected void AddApiErrors<T>(ApiResult<T> result)
    {
        if (result.Errors.Count > 0)
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
        else
            ModelState.AddModelError(string.Empty, result.Message);
    }

    /// <summary>Home page for a role: Organisers -> dashboard, Participants -> browse events.</summary>
    protected IActionResult RedirectToHomeFor(SessionUser user) =>
        user.IsOrganiser
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Events");
}

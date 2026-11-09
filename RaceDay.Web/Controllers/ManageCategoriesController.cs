using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: manage the age and distance categories of one event.</summary>
[RequireLogin(Roles.Organiser)]
[Route("Organiser/Events/{eventId:int}/Categories")]
public class ManageCategoriesController : RaceDayController
{
    private readonly ApiClient _api;

    public ManageCategoriesController(ApiClient api) => _api = api;

    // GET /Organiser/Events/5/Categories?edit=3
    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId, int? edit = null)
    {
        var model = await LoadAsync(eventId);
        if (model.Failed is { } failed) return failed;

        if (edit is not null)
        {
            var cat = model.Page!.Categories.FirstOrDefault(c => c.CategoryId == edit);
            if (cat is not null)
                model.Page.Form = new CategoryFormViewModel
                {
                    CategoryId = cat.CategoryId,
                    EventId = eventId,
                    CategoryName = cat.CategoryName,
                    CategoryType = cat.CategoryType,
                    Distance = cat.Distance,
                    MinAge = cat.MinAge,
                    MaxAge = cat.MaxAge,
                    MaxParticipants = cat.MaxParticipants,
                    EntryFee = cat.EntryFee
                };
        }

        return View(model.Page);
    }

    // POST /Organiser/Events/5/Categories/Save   (create when CategoryId = 0, otherwise update)
    [HttpPost("Save")]
    public async Task<IActionResult> Save(int eventId, CategoryFormViewModel form)
    {
        form.EventId = eventId;

        // Light checks so the user gets a friendly message; the API enforces the real rules.
        if (form.CategoryType == "Distance")
        {
            if (form.Distance is null) ModelState.AddModelError(nameof(form.Distance), "Distance categories need a distance in km.");
            ModelState.Remove(nameof(form.MinAge));
            ModelState.Remove(nameof(form.MaxAge));
        }
        else if (form.CategoryType == "Age")
        {
            if (form.MinAge is null) ModelState.AddModelError(nameof(form.MinAge), "Enter the minimum age.");
            if (form.MaxAge is null) ModelState.AddModelError(nameof(form.MaxAge), "Enter the maximum age.");
            if (form.MinAge is not null && form.MaxAge is not null && form.MinAge > form.MaxAge)
                ModelState.AddModelError(nameof(form.MaxAge), "Maximum age must be at least the minimum age.");
            ModelState.Remove(nameof(form.Distance));
        }
        else
        {
            ModelState.AddModelError(nameof(form.CategoryType), "Choose Age or Distance.");
        }

        if (!ModelState.IsValid)
        {
            var invalid = await LoadAsync(eventId);
            if (invalid.Failed is { } f) return f;
            invalid.Page!.Form = form;
            return View("Index", invalid.Page);
        }

        var request = new CategoryRequest
        {
            CategoryName = form.CategoryName.Trim(),
            CategoryType = form.CategoryType,
            Distance = form.CategoryType == "Distance" ? form.Distance : null,
            MinAge = form.CategoryType == "Age" ? form.MinAge : null,
            MaxAge = form.CategoryType == "Age" ? form.MaxAge : null,
            MaxParticipants = form.MaxParticipants,
            EntryFee = form.EntryFee
        };

        var result = form.CategoryId > 0
            ? await _api.UpdateCategoryAsync(form.CategoryId, request)
            : await _api.CreateCategoryAsync(eventId, request);

        if (result.Unauthorized) return SessionExpired();
        if (!result.Success)
        {
            var failedPage = await LoadAsync(eventId);
            if (failedPage.Failed is { } f) return f;
            failedPage.Page!.Form = form;
            AddApiErrors(result);
            return View("Index", failedPage.Page);
        }

        FlashSuccess(form.CategoryId > 0 ? "Category updated." : $"Category “{request.CategoryName}” added.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    // POST /Organiser/Events/5/Categories/Delete/3
    [HttpPost("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await _api.DeleteCategoryAsync(id);
        if (FailAction(result) is { } expired) return expired;
        if (result.Success) FlashSuccess("Category deleted.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<(ManageCategoriesViewModel? Page, IActionResult? Failed)> LoadAsync(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (FailPage(ev) is { } failedEvent) return (null, failedEvent);
        if (ev.Data!.OrganiserId != CurrentUser!.UserId)
            return (null, RedirectToAction("AccessDenied", "Account"));

        var cats = await _api.GetCategoriesAsync(eventId);
        if (FailPage(cats) is { } failedCats) return (null, failedCats);

        return (new ManageCategoriesViewModel
        {
            Event = ev.Data,
            Categories = cats.Data ?? new List<CategoryDto>(),
            Form = new CategoryFormViewModel { EventId = eventId }
        }, null);
    }
}

using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>View and edit your own profile, and upload a profile picture (both roles).</summary>
[RequireLogin]
public class ProfileController : RaceDayController
{
    private const long MaxPictureBytes = 5 * 1024 * 1024;
    private readonly ApiClient _api;

    public ProfileController(ApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyProfileAsync();
        if (FailPage(result) is { } failed) return failed;
        return View(ToModel(result.Data!));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return await Redisplay(model);

        var result = await _api.UpdateMyProfileAsync(new ProfileRequest
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim()
        });

        if (result.Unauthorized) return SessionExpired();
        if (!result.Success)
        {
            AddApiErrors(result);
            return await Redisplay(model);
        }

        HttpContext.Session.SetUser(result.Data!);
        FlashSuccess("Profile updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        // Friendly checks first; the API repeats them (size, type, real image bytes).
        if (picture is null || picture.Length == 0)
        {
            FlashError("Choose a picture to upload first.");
            return RedirectToAction(nameof(Index));
        }
        if (picture.Length > MaxPictureBytes)
        {
            FlashError("That picture is too big. Please choose one under 5 MB.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadMyPictureAsync(picture);
        if (result.Unauthorized) return SessionExpired();

        if (result.Success && result.Data is not null)
        {
            HttpContext.Session.SetPicture(result.Data.ProfilePictureUrl);
            FlashSuccess("Profile picture updated.");
        }
        else
        {
            FlashError(result.FullMessage);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Redisplay(ProfileViewModel posted)
    {
        // Role / picture / member-since aren't posted back, so reload them.
        var current = await _api.GetMyProfileAsync();
        if (current.Success && current.Data is not null)
        {
            posted.Role = current.Data.Role;
            posted.MemberSince = current.Data.CreatedAt;
            posted.ProfilePictureUrl = current.Data.ProfilePictureUrl;
        }
        return View("Index", posted);
    }

    private static ProfileViewModel ToModel(UserProfile p) => new()
    {
        FullName = p.FullName,
        Email = p.Email,
        Role = p.Role,
        MemberSince = p.CreatedAt,
        ProfilePictureUrl = p.ProfilePictureUrl
    };
}

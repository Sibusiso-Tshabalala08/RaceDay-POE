using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : RaceDayController
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api) => _api = api;

    // ---------- login ----------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentUser is { } user) return RedirectToHomeFor(user);
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Start from a clean session so nothing from a previous user survives
        // (and a login can't "adopt" a pre-existing session id).
        HttpContext.Session.Clear();

        var result = await _api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password });
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty,
                result.Unauthorized ? "Incorrect email or password." : result.FullMessage);
            model.Password = string.Empty;
            return View(model);
        }

        HttpContext.Session.SetUser(result.Data);
        FlashSuccess($"Welcome back, {result.Data.FullName.Split(' ')[0]}!");

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            && model.ReturnUrl.Length > 1 && !model.ReturnUrl.StartsWith("//"))
        {
            // Don't send an Organiser to a Participant-only page (or vice versa).
            return LocalRedirect(model.ReturnUrl);
        }
        return RedirectToHomeFor(CurrentUser!);
    }

    // ---------- register ----------
    [HttpGet]
    public IActionResult Register(string? role = null)
    {
        if (CurrentUser is { } user) return RedirectToHomeFor(user);
        return View(new RegisterViewModel { Role = role == "Organiser" ? "Organiser" : "Participant" });
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(Blank(model));

        var created = await _api.RegisterAsync(new RegisterRequest
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            Role = model.Role
        });

        if (!created.Success)
        {
            ModelState.AddModelError(string.Empty,
                created.Conflict ? "An account with that email already exists. Try logging in instead." : created.FullMessage);
            return View(Blank(model));
        }

        // Log the new user straight in.
        HttpContext.Session.Clear();
        var login = await _api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password });
        if (login.Success && login.Data is not null)
        {
            HttpContext.Session.SetUser(login.Data);
            FlashSuccess($"Welcome to RaceDay, {login.Data.FullName.Split(' ')[0]}! Your account is ready.");
            return RedirectToHomeFor(CurrentUser!);
        }

        FlashSuccess("Account created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    private static RegisterViewModel Blank(RegisterViewModel m)
    {
        m.Password = string.Empty;
        m.ConfirmPassword = string.Empty;
        return m;
    }

    // ---------- logout ----------
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (CurrentUser is not null)
            await _api.LogoutAsync();          // ends the API session; failure here doesn't matter

        HttpContext.Session.Clear();
        FlashInfo("You've been logged out. See you at the start line!");
        return RedirectToAction("Index", "Home");
    }

    // ---------- wrong role ----------
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }
}

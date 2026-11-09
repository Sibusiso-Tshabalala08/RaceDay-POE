using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Protects a controller or action.
///   [RequireLogin]               any logged-in user
///   [RequireLogin("Organiser")]  Organisers only
/// Not logged in  -> redirected to the login page (and sent back afterwards).
/// Wrong role     -> redirected to the friendly "access denied" page.
/// The API enforces the same rules again on every call, so this is for a good
/// user experience, not the only line of defence.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireLoginAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireLoginAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Session.GetUser();

        if (user is null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Method == HttpMethods.Get
                ? request.Path + request.QueryString
                : null;

            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(user.Role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

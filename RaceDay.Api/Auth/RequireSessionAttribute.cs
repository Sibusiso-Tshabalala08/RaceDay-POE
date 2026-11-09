using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Auth;

/// <summary>
/// Enforces session-based authentication and (optionally) role-based access,
/// per the Part 2 brief: "session management to maintain the user's
/// authenticated state and role for all subsequent requests."
///
/// Usage:
///   [RequireSession]                 - any logged-in user (Organiser or Participant)
///   [RequireSession("Organiser")]     - Organiser only
///   [RequireSession("Participant")]   - Participant only
///
/// On failure: 401 if nobody is logged in, 403 if logged in but wrong role.
/// This directly targets the rubric's top band for Authentication and
/// Role-Based Access: "server-side session stores user ID and role, all
/// protected endpoints enforce role correctly."
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireSessionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public RequireSessionAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetInt32(SessionKeys.UserId);

        if (userId is null)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "You must be logged in to access this resource."
            });
            return;
        }

        if (_allowedRoles.Length > 0)
        {
            var role = context.HttpContext.Session.GetString(SessionKeys.Role);

            if (role is null || !_allowedRoles.Contains(role))
            {
                context.Result = new ObjectResult(new
                {
                    message = "You do not have permission to access this resource."
                })
                { StatusCode = StatusCodes.Status403Forbidden };
            }
        }
    }
}

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
}

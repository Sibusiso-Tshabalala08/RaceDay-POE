using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Auth;

public static class SessionExtensions
{
    public static int GetUserId(this HttpContext context) =>
        context.Session.GetInt32(SessionKeys.UserId)
        ?? throw new InvalidOperationException("No authenticated user in session.");

    public static string GetRole(this HttpContext context) =>
        context.Session.GetString(SessionKeys.Role)
        ?? throw new InvalidOperationException("No role in session.");
}

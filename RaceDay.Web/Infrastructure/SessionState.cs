using RaceDay.Web.Models;

namespace RaceDay.Web.Infrastructure;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string Email = "Email";
    public const string ProfilePictureUrl = "ProfilePictureUrl";

    /// <summary>The "name=value" of the user's server-side session cookie on the API.</summary>
    public const string ApiCookie = "ApiCookie";
}

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Who is logged in, as remembered in the MVC session.</summary>
public record SessionUser(int UserId, string Role, string FullName, string Email, string? ProfilePictureUrl)
{
    public bool IsOrganiser => Role == Roles.Organiser;
    public bool IsParticipant => Role == Roles.Participant;
}

public static class SessionExtensions
{
    public static SessionUser? GetUser(this ISession session)
    {
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);
        if (userId is null || string.IsNullOrEmpty(role))
            return null;

        return new SessionUser(
            userId.Value,
            role,
            session.GetString(SessionKeys.FullName) ?? string.Empty,
            session.GetString(SessionKeys.Email) ?? string.Empty,
            session.GetString(SessionKeys.ProfilePictureUrl));
    }

    /// <summary>Stores the logged-in user's id and role (plus display details) in the session.</summary>
    public static void SetUser(this ISession session, UserProfile user)
    {
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.FullName, user.FullName);
        session.SetString(SessionKeys.Email, user.Email);
        SetPicture(session, user.ProfilePictureUrl);
    }

    public static void SetPicture(this ISession session, string? url)
    {
        if (string.IsNullOrEmpty(url))
            session.Remove(SessionKeys.ProfilePictureUrl);
        else
            session.SetString(SessionKeys.ProfilePictureUrl, url);
    }
}

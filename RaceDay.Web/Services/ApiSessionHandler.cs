using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Services;

/// <summary>
/// Keeps each user's API session alive across requests.
///
/// The API remembers who is logged in through an ASP.NET session cookie
/// (".AspNetCore.Session"). A browser would do this automatically, but the MVC
/// server is the API's client - so this handler does it by hand:
///   * after login, it captures the cookie the API sets and stores it in THIS
///     user's MVC session;
///   * on every later call, it sends that cookie back.
/// The HttpClient's own cookie jar is disabled in Program.cs, so one user's
/// cookie can never be sent on behalf of another.
/// </summary>
public class ApiSessionHandler : DelegatingHandler
{
    private const string ApiSessionCookieName = ".AspNetCore.Session";
    private readonly IHttpContextAccessor _accessor;

    public ApiSessionHandler(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _accessor.HttpContext?.Session;

        if (session is { IsAvailable: true })
        {
            var cookie = session.GetString(SessionKeys.ApiCookie);
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (session is { IsAvailable: true } &&
            response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                // "name=value; path=/; httponly" -> keep only "name=value"
                var pair = header.Split(';', 2)[0].Trim();
                if (pair.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal))
                {
                    // An empty value means the API cleared the session (logout).
                    if (pair.Length == ApiSessionCookieName.Length + 1)
                        session.Remove(SessionKeys.ApiCookie);
                    else
                        session.SetString(SessionKeys.ApiCookie, pair);
                }
            }
        }

        return response;
    }
}

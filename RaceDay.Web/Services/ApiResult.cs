using System.Net;

namespace RaceDay.Web.Services;

/// <summary>
/// What came back from one API call. Controllers never see an exception or a
/// raw HTTP status - they check Success / Unauthorized / Unavailable and show a
/// friendly message.
/// </summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public T? Data { get; init; }

    /// <summary>A message that is safe to show the user.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Field-level validation messages from the API, if any.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>The API could not be reached (down, timed out, wrong address).</summary>
    public bool Unavailable { get; init; }

    public bool Unauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool Forbidden => StatusCode == HttpStatusCode.Forbidden;
    public bool NotFound => StatusCode == HttpStatusCode.NotFound;
    public bool Conflict => StatusCode == HttpStatusCode.Conflict;

    public static ApiResult<T> Ok(T? data, HttpStatusCode status = HttpStatusCode.OK) =>
        new() { Success = true, Data = data, StatusCode = status };

    public static ApiResult<T> Fail(HttpStatusCode status, string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, StatusCode = status, Message = message, Errors = errors ?? Array.Empty<string>() };

    public static ApiResult<T> Down() =>
        new()
        {
            Success = false,
            Unavailable = true,
            StatusCode = HttpStatusCode.ServiceUnavailable,
            Message = "RaceDay can't reach its server right now. Please try again in a moment."
        };

    /// <summary>The message plus any field errors, as one line.</summary>
    public string FullMessage =>
        Errors.Count == 0 ? Message : string.Join(" ", Errors);
}

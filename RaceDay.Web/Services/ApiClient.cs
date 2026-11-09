using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>
/// Every call the MVC app makes to the Part 2 REST API goes through here.
/// One method per endpoint, each returning an <see cref="ApiResult{T}"/>.
/// </summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _log;

    public ApiClient(HttpClient http, ILogger<ApiClient> log)
    {
        _http = http;
        _log = log;
    }

    // ---------- auth & profile ----------
    public Task<ApiResult<UserProfile>> RegisterAsync(RegisterRequest body) => SendAsync<UserProfile>(HttpMethod.Post, "api/auth/register", body);
    /// <summary>The API replies to login with { "message": "...", "user": { ...profile... } }.</summary>
    private sealed class LoginEnvelope
    {
        public UserProfile? User { get; set; }
    }

    public async Task<ApiResult<UserProfile>> LoginAsync(LoginRequest body)
    {
        var r = await SendAsync<LoginEnvelope>(HttpMethod.Post, "api/auth/login", body);

        if (r.Success && r.Data?.User is { } profile)
            return ApiResult<UserProfile>.Ok(profile, r.StatusCode);

        return new ApiResult<UserProfile>
        {
            Success = false,
            StatusCode = r.Success ? HttpStatusCode.BadGateway : r.StatusCode,
            Message = r.Success ? "The server sent a login response RaceDay could not read." : r.Message,
            Errors = r.Errors,
            Unavailable = r.Unavailable
        };
    }
    public Task<ApiResult<object>> LogoutAsync() => SendAsync<object>(HttpMethod.Post, "api/auth/logout");
    public Task<ApiResult<UserProfile>> GetMyProfileAsync() => SendAsync<UserProfile>(HttpMethod.Get, "api/users/me");
    public Task<ApiResult<UserProfile>> UpdateMyProfileAsync(ProfileRequest body) => SendAsync<UserProfile>(HttpMethod.Put, "api/users/me", body);
    public Task<ApiResult<UserProfile>> UploadMyPictureAsync(IFormFile file) => UploadAsync<UserProfile>("api/users/me/picture", file);

    // ---------- events ----------
    public Task<ApiResult<List<EventDto>>> GetEventsAsync() => SendAsync<List<EventDto>>(HttpMethod.Get, "api/events");
    public Task<ApiResult<List<EventDto>>> GetMyEventsAsync() => SendAsync<List<EventDto>>(HttpMethod.Get, "api/events/mine");
    public Task<ApiResult<EventDto>> GetEventAsync(int id) => SendAsync<EventDto>(HttpMethod.Get, $"api/events/{id}");
    public Task<ApiResult<EventDto>> CreateEventAsync(EventRequest body) => SendAsync<EventDto>(HttpMethod.Post, "api/events", body);
    public Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventRequest body) => SendAsync<EventDto>(HttpMethod.Put, $"api/events/{id}", body);
    public Task<ApiResult<object>> DeleteEventAsync(int id) => SendAsync<object>(HttpMethod.Delete, $"api/events/{id}");
    public Task<ApiResult<EventDto>> UploadBannerAsync(int id, IFormFile file) => UploadAsync<EventDto>($"api/events/{id}/banner", file);

    /// <summary>Live weather for an event; null when there is no forecast (no coordinates, provider down).</summary>
    public async Task<WeatherInfo?> GetWeatherAsync(int eventId)
    {
        var r = await SendAsync<JsonElement>(HttpMethod.Get, $"api/events/{eventId}/weather");
        if (!r.Success || r.Data.ValueKind != JsonValueKind.Object) return null;
        try
        {
            if (!r.Data.TryGetProperty("current", out var cur)) return null;
            return new WeatherInfo
            {
                Temperature = Num(cur, "temperature_2m"),
                Precipitation = Num(cur, "precipitation"),
                WindSpeed = Num(cur, "wind_speed_10m")
            };
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not read weather JSON");
            return null;
        }
    }

    private static decimal? Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;

    // ---------- categories ----------
    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId) => SendAsync<List<CategoryDto>>(HttpMethod.Get, $"api/events/{eventId}/categories");
    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest body) => SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", body);
    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int id, CategoryRequest body) => SendAsync<CategoryDto>(HttpMethod.Put, $"api/categories/{id}", body);
    public Task<ApiResult<object>> DeleteCategoryAsync(int id) => SendAsync<object>(HttpMethod.Delete, $"api/categories/{id}");

    // ---------- enrolments ----------
    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId) => SendAsync<EnrolmentDto>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments");
    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() => SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/users/me/enrolments");
    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) => SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");
    public Task<ApiResult<object>> CancelEnrolmentAsync(int id) => SendAsync<object>(HttpMethod.Delete, $"api/enrolments/{id}");

    // ---------- results ----------
    public Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId) => SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/events/{eventId}/results");
    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync() => SendAsync<List<ResultDto>>(HttpMethod.Get, "api/users/me/results");
    public Task<ApiResult<ResultDto>> CreateResultAsync(int enrolmentId, ResultRequest body) => SendAsync<ResultDto>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/results", body);
    public Task<ApiResult<ResultDto>> UpdateResultAsync(int id, ResultRequest body) => SendAsync<ResultDto>(HttpMethod.Put, $"api/results/{id}", body);

    // ---------- plumbing ----------
    private Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var stream = file.OpenReadStream();
        var part = new StreamContent(stream);
        part.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        form.Add(part, "file", Path.GetFileName(file.FileName));
        return SendAsync<T>(HttpMethod.Post, path, content: form);
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (content is not null) request.Content = content;
            else if (body is not null) request.Content = JsonContent.Create(body, options: Json);

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent ||
                    response.Content.Headers.ContentLength == 0 ||
                    typeof(T) == typeof(object))
                    return ApiResult<T>.Ok(default, response.StatusCode);

                var data = await response.Content.ReadFromJsonAsync<T>(Json);
                return ApiResult<T>.Ok(data, response.StatusCode);
            }

            var (message, errors) = await ReadErrorAsync(response);
            return ApiResult<T>.Fail(response.StatusCode, message, errors);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _log.LogWarning(ex, "API call {Method} {Path} failed", method, path);
            return ApiResult<T>.Down();
        }
        catch (JsonException ex)
        {
            _log.LogError(ex, "API call {Method} {Path} returned unreadable JSON", method, path);
            return ApiResult<T>.Fail(HttpStatusCode.BadGateway, "The server sent a response RaceDay could not read.");
        }
    }

    /// <summary>Reads { "message": "..." } or the ASP.NET validation format { "errors": { "Field": ["..."] } }.</summary>
    private static async Task<(string Message, IReadOnlyList<string> Errors)> ReadErrorAsync(HttpResponseMessage response)
    {
        string fallback = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with existing data.",
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway => "A required service is temporarily unavailable.",
            _ => "Something went wrong. Please check your details and try again."
        };

        try
        {
            var doc = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
            if (doc.ValueKind != JsonValueKind.Object) return (fallback, Array.Empty<string>());

            var errors = new List<string>();
            if (doc.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
                foreach (var field in errs.EnumerateObject())
                    if (field.Value.ValueKind == JsonValueKind.Array)
                        foreach (var m in field.Value.EnumerateArray())
                            if (m.GetString() is { Length: > 0 } s) errors.Add(s);

            if (doc.TryGetProperty("message", out var msg) && msg.GetString() is { Length: > 0 } text)
                return (text, errors);

            return (errors.Count > 0 ? "Please fix the highlighted details." : fallback, errors);
        }
        catch
        {
            return (fallback, Array.Empty<string>());
        }
    }
}

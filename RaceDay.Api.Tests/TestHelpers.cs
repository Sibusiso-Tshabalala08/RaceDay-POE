using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

/// <summary>Wraps the { message, user } shape returned by POST /api/auth/login.</summary>
public record LoginResponse(string Message, UserProfileDto User);

public static class TestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client, string fullName, string email, string password, string role)
    {
        return await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName,
            email,
            password,
            role
        });
    }

    /// <summary>
    /// Registers and logs in a fresh user, leaving the session cookie on the
    /// given HttpClient so subsequent calls on that same client are
    /// authenticated as this user. Returns the logged-in profile.
    /// </summary>
    public static async Task<UserProfileDto> RegisterAndLoginAsync(
        HttpClient client, string role, string? emailOverride = null)
    {
        var email = emailOverride ?? $"{Guid.NewGuid():N}@example.com";

        var registerResponse = await RegisterAsync(client, $"Test {role}", email, "Password123!", role);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return body!.User;
    }

    public static async Task<EventResponseDto> CreateEventAsync(HttpClient organiserClient)
    {
        var response = await organiserClient.PostAsJsonAsync("/api/events", new
        {
            eventName = "Test Marathon",
            eventDate = DateTime.UtcNow.AddMonths(2),
            location = "Test City",
            latitude = -26.2041,
            longitude = 28.0473,
            distance = 42.2,
            eventType = "Run",
            description = "A test event."
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponseDto>(JsonOptions))!;
    }

    public static async Task<CategoryResponseDto> CreateCategoryAsync(
        HttpClient organiserClient, int eventId, int maxParticipants = 100)
    {
        var response = await organiserClient.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10km",
            categoryType = "Distance",
            distance = 10.0,
            maxParticipants,
            entryFee = 150.0
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponseDto>(JsonOptions))!;
    }
}

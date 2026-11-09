using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Covers the brief's required area: "User registration and login" —
/// both expected success and expected failure scenarios.
/// </summary>
public class AuthTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public AuthTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidOrganiserDetails_Returns201()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.RegisterAsync(
            client, "Jane Organiser", $"{Guid.NewGuid():N}@example.com", "Password123!", "Organiser");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithValidParticipantDetails_Returns201()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.RegisterAsync(
            client, "Jane Participant", $"{Guid.NewGuid():N}@example.com", "Password123!", "Participant");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        await TestHelpers.RegisterAsync(client, "First User", email, "Password123!", "Participant");
        var secondAttempt = await TestHelpers.RegisterAsync(client, "Second User", email, "Password123!", "Participant");

        secondAttempt.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.RegisterAsync(
            client, "Bad Role", $"{Guid.NewGuid():N}@example.com", "Password123!", "Administrator");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndStartsSession()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        await TestHelpers.RegisterAsync(client, "Login Test", email, "Password123!", "Participant");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Session cookie should now let an authenticated-only endpoint succeed.
        var me = await client.GetAsync("/api/users/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        await TestHelpers.RegisterAsync(client, "Wrong Password", email, "Password123!", "Participant");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyProfile_WithoutLoggingIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

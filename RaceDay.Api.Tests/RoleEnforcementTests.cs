using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Covers the brief's required area: "Different roles are rejected when
/// attempting to access role-based functionalities" — i.e. Organiser-only
/// endpoints reject Participants and vice versa, and protected endpoints
/// reject anonymous requests.
/// </summary>
public class RoleEnforcementTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public RoleEnforcementTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Participant");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Participant Attempt",
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Nowhere",
            eventType = "Run"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var organiserClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiserClient);

        var participantClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participantClient, "Participant");

        var response = await participantClient.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new
        {
            categoryName = "5km",
            categoryType = "Distance",
            distance = 5.0,
            maxParticipants = 50,
            entryFee = 50.0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInCategory_Returns403()
    {
        var organiserClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiserClient);
        var category = await TestHelpers.CreateCategoryAsync(organiserClient, ev.EventId);

        // Same Organiser tries to use a Participant-only endpoint.
        var response = await organiserClient.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolmentsList_Returns403()
    {
        var organiserClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiserClient);

        var participantClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participantClient, "Participant");

        // GET /api/events/{id}/enrolments is Organiser-only.
        var response = await participantClient.GetAsync($"/api/events/{ev.EventId}/enrolments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnonymousUser_CannotCreateEvent_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Anonymous Attempt",
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Nowhere",
            eventType = "Run"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

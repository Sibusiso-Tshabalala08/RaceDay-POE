using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Covers the brief's required area: "Event management - verifying that
/// Organisers can manage events" — create, update, delete, and viewing,
/// including success and failure scenarios.
/// </summary>
public class EventManagementTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EventManagementTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Durban Beach Run",
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Durban",
            latitude = -29.8587,
            longitude = 31.0218,
            distance = 10.0,
            eventType = "Run",
            description = "A scenic coastal run."
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<EventResponseDto>();
        created!.EventName.Should().Be("Durban Beach Run");
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(client);

        var response = await client.PutAsJsonAsync($"/api/events/{ev.EventId}", new
        {
            eventName = "Updated Marathon Name",
            eventDate = ev.EventDate,
            location = ev.Location,
            latitude = ev.Latitude,
            longitude = ev.Longitude,
            distance = ev.Distance,
            eventType = ev.EventType,
            description = "Updated description."
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<EventResponseDto>();
        updated!.EventName.Should().Be("Updated Marathon Name");
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_Returns204()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(client);

        var response = await client.DeleteAsync($"/api/events/{ev.EventId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Organiser_CannotEditAnotherOrganisersEvent_Returns403()
    {
        var ownerClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(ownerClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(ownerClient);

        var otherClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(otherClient, "Organiser");

        var response = await otherClient.PutAsJsonAsync($"/api/events/{ev.EventId}", new
        {
            eventName = "Hijacked Name",
            eventDate = ev.EventDate,
            location = ev.Location,
            eventType = ev.EventType
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Bad Type Event",
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Nowhere",
            eventType = "Swim" // not Run/Walk/Cycle
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllEvents_WithoutLoggingIn_Returns200()
    {
        // Browsing events is public per the Part 1 plan.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

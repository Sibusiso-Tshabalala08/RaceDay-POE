using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Part 3: the extra data the MVC front end needs - the Organiser dashboard
/// summary (GET /api/events/mine) and a Participant's personal results
/// with event details and the number of finishers.
/// </summary>
public class OrganiserSummaryTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public OrganiserSummaryTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MyEvents_ReturnsOnlyOwnEvents_WithEnrolmentCounts()
    {
        var organiserA = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserA, "Organiser");
        var eventA = await TestHelpers.CreateEventAsync(organiserA);
        var category = await TestHelpers.CreateCategoryAsync(organiserA, eventA.EventId);

        var organiserB = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserB, "Organiser");
        await TestHelpers.CreateEventAsync(organiserB);

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");
        var enrolResponse = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        enrolResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await organiserA.GetAsync("/api/events/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<EventSummaryDto>>();
        events.Should().ContainSingle();
        events![0].EventId.Should().Be(eventA.EventId);
        events[0].EnrolmentCount.Should().Be(1);
        events[0].CategoryCount.Should().Be(1);
    }

    [Fact]
    public async Task MyEvents_AsParticipant_Returns403()
    {
        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.GetAsync("/api/events/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MyEvents_WithoutLoggingIn_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/events/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MyResults_IncludeEventDetailsAndFinisherCount()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");
        var enrolResponse = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

        var resultResponse = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment!.EnrolmentId}/results",
            new { finishTime = "00:50:00", position = 1, status = "Finished" });
        resultResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var mine = await participant.GetFromJsonAsync<List<ResultResponseDto>>("/api/users/me/results");

        mine.Should().ContainSingle();
        mine![0].EventName.Should().Be(ev.EventName);
        mine[0].Position.Should().Be(1);
        mine[0].TotalFinishers.Should().Be(1);
        mine[0].Status.Should().Be("Finished");
    }
}

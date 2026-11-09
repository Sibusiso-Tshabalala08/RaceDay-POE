using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Covers the brief's required area: "Event enrolments - verifying that
/// Participants can enrol and that the enrolment is correctly recorded."
/// </summary>
public class EnrolmentTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EnrolmentTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CanEnrolInCategory_Returns201AndRecordsCorrectLink()
    {
        var organiserClient = _factory.CreateClient();
        var organiser = await TestHelpers.RegisterAndLoginAsync(organiserClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiserClient);
        var category = await TestHelpers.CreateCategoryAsync(organiserClient, ev.EventId);

        var participantClient = _factory.CreateClient();
        var participant = await TestHelpers.RegisterAndLoginAsync(participantClient, "Participant");

        var response = await participantClient.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

        // Confirms the enrolment correctly links Participant + Category + Event,
        // exactly as required: "record the link between the Participant, the
        // event, and the selected category."
        enrolment!.ParticipantId.Should().Be(participant.UserId);
        enrolment.CategoryId.Should().Be(category.CategoryId);
        enrolment.EventId.Should().Be(ev.EventId);
        enrolment.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task Participant_EnrolledInCategory_SeesItInOwnEnrolmentsList()
    {
        var organiserClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiserClient);
        var category = await TestHelpers.CreateCategoryAsync(organiserClient, ev.EventId);

        var participantClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participantClient, "Participant");
        await participantClient.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        var response = await participantClient.GetAsync("/api/users/me/enrolments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentResponseDto>>();
        enrolments.Should().ContainSingle(e => e.CategoryId == category.CategoryId);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameCategory_Returns409()
    {
        var organiserClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiserClient);
        var category = await TestHelpers.CreateCategoryAsync(organiserClient, ev.EventId);

        var participantClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participantClient, "Participant");

        var first = await participantClient.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var second = await participantClient.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Participant_CannotEnrolInFullCategory_Returns400()
    {
        var organiserClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiserClient);
        // MaxParticipants = 1, so the second enrolment attempt must fail.
        var category = await TestHelpers.CreateCategoryAsync(organiserClient, ev.EventId, maxParticipants: 1);

        var firstParticipant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(firstParticipant, "Participant");
        var firstResponse = await firstParticipant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondParticipant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(secondParticipant, "Participant");
        var secondResponse = await secondParticipant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Organiser_CanViewAllEnrolmentsForOwnEvent()
    {
        var organiserClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiserClient, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiserClient);
        var category = await TestHelpers.CreateCategoryAsync(organiserClient, ev.EventId);

        var participantClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participantClient, "Participant");
        await participantClient.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        var response = await organiserClient.GetAsync($"/api/events/{ev.EventId}/enrolments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentResponseDto>>();
        enrolments.Should().ContainSingle(e => e.CategoryId == category.CategoryId);
    }
}

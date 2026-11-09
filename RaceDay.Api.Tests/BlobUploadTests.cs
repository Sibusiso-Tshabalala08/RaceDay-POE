using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using RaceDay.Api.DTOs;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Part 3: image uploads. The real Azure Blob Storage service is replaced by
/// a fake (see RaceDayWebApplicationFactory), so these tests check the API's
/// own rules - who may upload, what counts as a valid image, and that the
/// returned blob URL is saved in the database.
/// </summary>
public class BlobUploadTests : IClassFixture<RaceDayWebApplicationFactory>
{
    // A real PNG signature followed by a few padding bytes.
    private static readonly byte[] PngBytes =
        { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 };

    private readonly RaceDayWebApplicationFactory _factory;

    public BlobUploadTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent ImageForm(byte[] bytes, string contentType, string fileName)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Organiser_CanUploadBanner_UrlIsSavedOnTheEvent()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(client);

        var response = await client.PostAsync($"/api/events/{ev.EventId}/banner",
            ImageForm(PngBytes, "image/png", "banner.png"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<EventResponseDto>();
        updated!.BannerImageUrl.Should().StartWith("https://fake.blob.core.windows.net/");

        // The URL must have been saved, not just returned once.
        var fetched = await client.GetFromJsonAsync<EventResponseDto>($"/api/events/{ev.EventId}");
        fetched!.BannerImageUrl.Should().Be(updated.BannerImageUrl);
    }

    [Fact]
    public async Task Participant_CannotUploadBanner_Returns403()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.PostAsync($"/api/events/{ev.EventId}/banner",
            ImageForm(PngBytes, "image/png", "banner.png"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organiser_CannotUploadBannerForSomeoneElsesEvent_Returns403()
    {
        var owner = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(owner, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var otherOrganiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(otherOrganiser, "Organiser");

        var response = await otherOrganiser.PostAsync($"/api/events/{ev.EventId}/banner",
            ImageForm(PngBytes, "image/png", "banner.png"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadBanner_WithoutLoggingIn_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync("/api/events/1/banner",
            ImageForm(PngBytes, "image/png", "banner.png"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadBanner_WithNonImageContentType_Returns400()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(client);

        var response = await client.PostAsync($"/api/events/{ev.EventId}/banner",
            ImageForm(Encoding.UTF8.GetBytes("just some text"), "text/plain", "notes.txt"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadBanner_WhenFileIsNotReallyAnImage_Returns400()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");
        var ev = await TestHelpers.CreateEventAsync(client);

        // Claims to be a PNG, but the bytes are plain text.
        var response = await client.PostAsync($"/api/events/{ev.EventId}/banner",
            ImageForm(Encoding.UTF8.GetBytes("definitely not a png file"), "image/png", "fake.png"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Participant_CanUploadProfilePicture_ProfileShowsTheUrl()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Participant");

        var response = await client.PostAsync("/api/users/me/picture",
            ImageForm(PngBytes, "image/png", "me.png"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");
        profile!.ProfilePictureUrl.Should().StartWith("https://fake.blob.core.windows.net/");
    }

    [Fact]
    public async Task UploadProfilePicture_WithoutLoggingIn_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync("/api/users/me/picture",
            ImageForm(PngBytes, "image/png", "me.png"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

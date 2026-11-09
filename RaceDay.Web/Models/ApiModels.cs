namespace RaceDay.Web.Models;

// These classes mirror the JSON the Part 2 API sends and receives (camelCase on
// the wire). They carry data only - no business rules live in the MVC project.

public class UserProfile
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? RouteMapUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? Description { get; set; }

    // Only filled by GET /api/events/mine
    public int EnrolmentCount { get; set; }
    public int CategoryCount { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public decimal? Distance { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }

    /// <summary>"10 km" for distance categories, "18–34 yrs" for age categories.</summary>
    public string Criteria => CategoryType == "Distance"
        ? Infrastructure.Fmt.Distance(Distance)
        : $"{MinAge}–{MaxAge} yrs";
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime? EventDate { get; set; }
    public string EventLocation { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime? EventDate { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalFinishers { get; set; }
}

// ---- request bodies sent to the API ----

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class EventRequest
{
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryType { get; set; } = string.Empty;
    public decimal? Distance { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int MaxParticipants { get; set; } = 100;
    public decimal EntryFee { get; set; }
}

public class ResultRequest
{
    public string? FinishTime { get; set; }   // "hh:mm:ss"
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";
}

public class ProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>Weather summary parsed from the API's Open-Meteo passthrough.</summary>
public class WeatherInfo
{
    public decimal? Temperature { get; set; }
    public decimal? Precipitation { get; set; }
    public decimal? WindSpeed { get; set; }
}

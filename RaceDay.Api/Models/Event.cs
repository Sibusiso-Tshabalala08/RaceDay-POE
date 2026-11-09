namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }

    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }

    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;

    // Added in Part 1 to support the live weather feature described in the
    // background brief. Not consumed until the GET /weather endpoint below.
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Required by the Part 2 functional spec: "Each event must capture a
    // name, description, date, location, distance, and event type."
    public decimal? Distance { get; set; }
    public string EventType { get; set; } = string.Empty; // Run / Walk / Cycle

    // Added in Part 1 to support Azure Blob Storage integration in Part 3.
    // Deliberately left unused by any Part 2 endpoint — see README for why.
    public string? RouteMapUrl { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

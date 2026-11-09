using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventCreateDto
{
    [Required] public string EventName { get; set; } = string.Empty;
    [Required] public DateTime EventDate { get; set; }
    [Required] public string Location { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Distance { get; set; }

    /// <summary>Must be exactly "Run", "Walk", or "Cycle".</summary>
    [Required] public string EventType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class EventResponseDto
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
    public string? Description { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ResultCreateDto
{
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    /// <summary>"Finished", "DNF", "DNS", or "DQ".</summary>
    [Required] public string Status { get; set; } = "Finished";
}

public class ResultResponseDto
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

    /// <summary>How many participants finished the event (set on "my results"), so the UI can show e.g. "47th out of 312 finishers".</summary>
    public int TotalFinishers { get; set; }
}

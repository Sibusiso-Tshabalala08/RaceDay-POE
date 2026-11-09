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
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
}

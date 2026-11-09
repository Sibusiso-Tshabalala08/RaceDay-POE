namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    // Nullable because a DNF/DNS/DQ result has no finish time or position.
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    /// <summary>"Finished", "DNF", "DNS", or "DQ".</summary>
    public string Status { get; set; } = "Finished";

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

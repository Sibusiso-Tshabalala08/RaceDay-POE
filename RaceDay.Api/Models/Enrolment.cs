namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }
    public User? Participant { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    /// <summary>"Confirmed" or "Cancelled".</summary>
    public string Status { get; set; } = "Confirmed";

    // One-to-one: a single enrolment produces at most one result.
    public Result? Result { get; set; }
}

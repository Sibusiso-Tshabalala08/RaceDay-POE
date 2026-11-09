namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    /// <summary>"Age" or "Distance" — see DbContext check constraint.</summary>
    public string CategoryType { get; set; } = string.Empty;

    // Exactly one of (Distance) or (MinAge, MaxAge) is populated, depending
    // on CategoryType. Enforced with a CHECK constraint in OnModelCreating.
    public decimal? Distance { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    public int MaxParticipants { get; set; } = 100;
    public decimal EntryFee { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

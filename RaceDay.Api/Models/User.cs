namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Stores a BCrypt hash — the raw password is never persisted.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    // Part 3: public URL of the profile picture stored in Azure Blob Storage.
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

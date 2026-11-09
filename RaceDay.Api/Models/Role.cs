namespace RaceDay.Api.Models;

/// <summary>
/// Lookup table: Organiser / Participant. Matches Part 1 ERD exactly.
/// </summary>
public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}

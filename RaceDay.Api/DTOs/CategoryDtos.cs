using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryCreateDto
{
    [Required] public string CategoryName { get; set; } = string.Empty;

    /// <summary>Must be exactly "Age" or "Distance".</summary>
    [Required] public string CategoryType { get; set; } = string.Empty;

    public decimal? Distance { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    public int MaxParticipants { get; set; } = 100;
    public decimal EntryFee { get; set; }
}

public class CategoryResponseDto
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
}

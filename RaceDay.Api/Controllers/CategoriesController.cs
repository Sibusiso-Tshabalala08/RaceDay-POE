using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    private static CategoryResponseDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        CategoryType = c.CategoryType,
        Distance = c.Distance,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
        MaxParticipants = c.MaxParticipants,
        EntryFee = c.EntryFee
    };

    private static bool TypeFieldsValid(CategoryCreateDto dto) => dto.CategoryType switch
    {
        "Distance" => dto.Distance is not null && dto.MinAge is null && dto.MaxAge is null,
        "Age" => dto.Distance is null && dto.MinAge is not null && dto.MaxAge is not null,
        _ => false
    };

    /// <summary>Lists all categories (age-based or distance-based) for a given event.</summary>
    /// <remarks>No login required. Returns 200 with an array of categories, or 404 if the event doesn't exist.</remarks>
    [HttpGet("api/events/{eventId}/categories")]
    public async Task<IActionResult> GetCategoriesForEvent(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
            return NotFound(new { message = "Event not found." });

        var categories = await _context.Categories.Where(c => c.EventId == eventId).ToListAsync();
        return Ok(categories.Select(ToDto));
    }

    /// <summary>Adds a new category (age-based or distance-based) to an event owned by the logged-in Organiser.</summary>
    /// <remarks>Organiser only, and only for events they created. Expects: categoryName, categoryType ("Age" or "Distance"), distance, minAge, maxAge, maxParticipants, entryFee. Returns 201 with the new category, 400 for an invalid type/field combination, 403 if the caller doesn't own the event, or 404 if the event doesn't exist.</remarks>
    [HttpPost("api/events/{eventId}/categories")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryCreateDto dto)
    {
        if (dto.CategoryType != "Age" && dto.CategoryType != "Distance")
            return BadRequest(new { message = "CategoryType must be 'Age' or 'Distance'." });

        if (!TypeFieldsValid(dto))
            return BadRequest(new { message = "Distance categories need a Distance and no ages; Age categories need MinAge and MaxAge and no distance." });

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only add categories to events you created." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName,
            CategoryType = dto.CategoryType,
            Distance = dto.Distance,
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            MaxParticipants = dto.MaxParticipants,
            EntryFee = dto.EntryFee
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(201, ToDto(category));
    }

    /// <summary>Updates a category belonging to an event the logged-in Organiser owns.</summary>
    /// <remarks>Organiser only. Returns 200 with the updated category, 400 for an invalid type/field combination, 403 if the caller doesn't own the parent event, or 404 if the category doesn't exist.</remarks>
    [HttpPut("api/categories/{id}")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> UpdateCategory(int id, CategoryCreateDto dto)
    {
        if (dto.CategoryType != "Age" && dto.CategoryType != "Distance")
            return BadRequest(new { message = "CategoryType must be 'Age' or 'Distance'." });

        if (!TypeFieldsValid(dto))
            return BadRequest(new { message = "Distance categories need a Distance and no ages; Age categories need MinAge and MaxAge and no distance." });

        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
            return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only edit categories on events you created." });

        category.CategoryName = dto.CategoryName;
        category.CategoryType = dto.CategoryType;
        category.Distance = dto.Distance;
        category.MinAge = dto.MinAge;
        category.MaxAge = dto.MaxAge;
        category.MaxParticipants = dto.MaxParticipants;
        category.EntryFee = dto.EntryFee;

        await _context.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    /// <summary>Deletes a category, as long as it has no active enrolments.</summary>
    /// <remarks>Organiser only, and only for events they created. Returns 204 on success, 403 if the caller doesn't own the parent event, 404 if the category doesn't exist, or 409 if participants are already enrolled.</remarks>
    [HttpDelete("api/categories/{id}")]
    [RequireSession("Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
            return NotFound(new { message = "Category not found." });

        if (category.Event!.OrganiserId != HttpContext.GetUserId())
            return StatusCode(403, new { message = "You can only delete categories on events you created." });

        if (category.Enrolments.Any())
            return Conflict(new { message = "This category has existing enrolments and cannot be deleted." });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

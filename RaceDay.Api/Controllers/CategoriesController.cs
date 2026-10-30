using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
public class CategoriesController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    /// <summary>List the categories (distances) for an event (public).</summary>
    /// <response code="200">Categories for the event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var cats = await _db.Categories.Where(c => c.EventId == eventId).OrderBy(c => c.DistanceKm).ToListAsync();
        return Ok(cats.Select(ToResponse).ToList());
    }

    /// <summary>Add a category such as "10km" or "21km" to an event you own (Organiser only).</summary>
    /// <response code="201">Category created.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only add categories to your own events.");

        var cat = new Category { EventId = eventId };
        Apply(cat, request);
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, ToResponse(cat));
    }

    /// <summary>Update a category of an event you own (Organiser only).</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the parent event belongs to another Organiser.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return NotFound();
        if (cat.Event!.OrganiserId != CurrentUserId) return Forbidden("You can only change categories of your own events.");

        Apply(cat, request);
        await _db.SaveChangesAsync();
        return Ok(ToResponse(cat));
    }

    /// <summary>Delete a category of an event you own (Organiser only).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the parent event belongs to another Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Participants are already enrolled in this category.</response>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return NotFound();
        if (cat.Event!.OrganiserId != CurrentUserId) return Forbidden("You can only change categories of your own events.");
        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Conflict(new { message = "Participants are already enrolled in this category." });

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Category cat, CategoryRequest r)
    {
        cat.CategoryName = r.CategoryName;
        cat.DistanceKm = r.DistanceKm;
        cat.Price = r.Price;
        cat.MaxParticipants = r.MaxParticipants;
    }

    internal static CategoryResponse ToResponse(Category c) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.DistanceKm, c.Price, c.MaxParticipants);
}

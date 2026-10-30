using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
public class EnrolmentsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public EnrolmentsController(AppDbContext db) => _db = db;

    /// <summary>Enter an event by enrolling in one of its categories (Participant only).</summary>
    /// <remarks>Records the link between you, the category and (through it) the event.</remarks>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can enter events.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">You are already enrolled in this category, or it is full.</response>
    [HttpPost("enrolments")]
    [SessionAuthorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolRequest request)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
        if (category is null) return NotFound(new { message = "Category not found." });

        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == CurrentUserId && e.CategoryId == request.CategoryId))
            return Conflict(new { message = "You are already enrolled in this category." });

        if (category.MaxParticipants > 0 &&
            await _db.Enrolments.CountAsync(e => e.CategoryId == request.CategoryId) >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        var enrolment = new Enrolment { ParticipantId = CurrentUserId, CategoryId = request.CategoryId };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var created = await Query().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return StatusCode(StatusCodes.Status201Created, ToResponse(created));
    }

    /// <summary>View your own enrolments (Participant only).</summary>
    /// <response code="200">Your enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants have enrolments.</response>
    [HttpGet("enrolments/me")]
    [SessionAuthorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var list = await Query().Where(e => e.ParticipantId == CurrentUserId).ToListAsync();
        return Ok(list.Select(ToResponse).ToList());
    }

    /// <summary>View the roster (all enrolments) for an event you own (Organiser only).</summary>
    /// <response code="200">Enrolments for the event, with participant details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only view enrolments for your own events.");

        var list = await Query().Where(e => e.Category!.EventId == eventId).ToListAsync();
        return Ok(list.Select(ToResponse).ToList());
    }

    /// <summary>Cancel your own enrolment (Participant only).</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or the enrolment belongs to someone else.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [SessionAuthorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound();
        if (enrolment.ParticipantId != CurrentUserId) return Forbidden("You can only cancel your own enrolments.");
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Enrolment> Query() =>
        _db.Enrolments.Include(e => e.Participant).Include(e => e.Category).ThenInclude(c => c!.Event);

    private static EnrolmentResponse ToResponse(Enrolment e) =>
        new(e.EnrolmentId, e.ParticipantId, e.Participant!.FullName, e.CategoryId, e.Category!.CategoryName,
            e.Category.EventId, e.Category.Event!.EventName, e.EnrolmentDate, e.Status);
}

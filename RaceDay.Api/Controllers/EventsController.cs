using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public EventsController(AppDbContext db) => _db = db;

    /// <summary>List all events (public). Use <c>upcoming=true</c> to show only future events.</summary>
    /// <response code="200">Events, soonest first.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? upcoming = null)
    {
        var query = _db.Events.AsQueryable();
        if (upcoming == true) query = query.Where(e => e.EventDate >= DateTime.Now);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToResponse).ToList());
    }

    /// <summary>Get the full details of one event, including its categories (public).</summary>
    /// <response code="200">The event and its categories.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();

        return Ok(new EventDetailResponse(ev.EventId, ev.OrganiserId, ev.EventName, ev.Description,
            ev.EventDate, ev.Location, ev.RouteInfo, ev.ImageUrl, ev.DistanceKm, ev.EventType, ev.CreatedAt,
            ev.Categories.Select(CategoriesController.ToResponse).ToList()));
    }

    /// <summary>Create an event (Organiser only): name, description, date, location, route info, distance and type (Run, Walk, Cycle).</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can create events.</response>
    [HttpPost]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        var ev = new Event { OrganiserId = CurrentUserId };
        Apply(ev, request);
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToResponse(ev));
    }

    /// <summary>Update an event you own (Organiser only).</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only change your own events.");

        Apply(ev, request);
        await _db.SaveChangesAsync();
        return Ok(ToResponse(ev));
    }

    /// <summary>Delete an event you own (Organiser only). Its categories are removed too.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event with that id.</response>
    /// <response code="409">Participants are enrolled, so the event cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only delete your own events.");

        var categoryIds = ev.Categories.Select(c => c.CategoryId).ToList();
        if (await _db.Enrolments.AnyAsync(e => categoryIds.Contains(e.CategoryId)))
            return Conflict(new { message = "Participants are already enrolled in this event." });

        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Event ev, EventRequest r)
    {
        ev.EventName = r.EventName;
        ev.Description = r.Description;
        ev.EventDate = r.EventDate;
        ev.Location = r.Location;
        ev.RouteInfo = r.RouteInfo;
        ev.ImageUrl = r.ImageUrl;
        ev.DistanceKm = r.DistanceKm;
        ev.EventType = r.EventType;
    }

    private static EventResponse ToResponse(Event e) =>
        new(e.EventId, e.OrganiserId, e.EventName, e.Description, e.EventDate, e.Location,
            e.RouteInfo, e.ImageUrl, e.DistanceKm, e.EventType, e.CreatedAt);
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
public class ResultsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public ResultsController(AppDbContext db) => _db = db;

    /// <summary>Capture a participant's finish time and finishing position (Organiser only).</summary>
    /// <remarks>Send the <c>enrolmentId</c>, a <c>finishTime</c> such as "01:23:45", the <c>position</c> and a <c>status</c> (for example Finished).</remarks>
    /// <response code="201">Result captured.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the enrolment is for another Organiser's event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("results")]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(CaptureResultRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Category!.Event!.OrganiserId != CurrentUserId)
            return Forbidden("You can only capture results for your own events.");
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var created = await Query().FirstAsync(r => r.ResultId == result.ResultId);
        return StatusCode(StatusCodes.Status201Created, ToResponse(created));
    }

    /// <summary>Update or correct a captured result for one of your events (Organiser only).</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the result is for another Organiser's event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id:int}")]
    [SessionAuthorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateResultRequest request)
    {
        var result = await Query().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result is null) return NotFound();
        if (result.Enrolment!.Category!.Event!.OrganiserId != CurrentUserId)
            return Forbidden("You can only change results for your own events.");

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = request.Status;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(result));
    }

    /// <summary>View your own result history across events (Participant only).</summary>
    /// <response code="200">Your results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants have personal results.</response>
    [HttpGet("results/me")]
    [SessionAuthorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var list = await Query().Where(r => r.Enrolment!.ParticipantId == CurrentUserId).ToListAsync();
        return Ok(list.Select(ToResponse).ToList());
    }

    /// <summary>View the results (leaderboard) for an event (public).</summary>
    /// <response code="200">Results ordered by position.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var list = await Query().Where(r => r.Enrolment!.Category!.EventId == eventId)
            .OrderBy(r => r.Position == null).ThenBy(r => r.Position).ToListAsync();
        return Ok(list.Select(ToResponse).ToList());
    }

    private IQueryable<Result> Query() =>
        _db.Results.Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
                   .Include(r => r.Enrolment).ThenInclude(e => e!.Category).ThenInclude(c => c!.Event);

    private static ResultResponse ToResponse(Result r) =>
        new(r.ResultId, r.EnrolmentId, r.Enrolment!.Category!.EventId, r.Enrolment.Category.Event!.EventName,
            r.Enrolment.Participant!.FullName, r.Enrolment.Category.CategoryName,
            r.FinishTime, r.Position, r.Status, r.CapturedAt);
}

using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RaceDayAuthorize]
[Route("events")]
public class EventsController : RaceDayController
{
    public EventsController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    /// <summary>Full event information, banner and categories (with Enrol buttons for Participants).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var eventTask = Api.GetEventAsync(id);
        var enrolmentsTask = Me.IsParticipant
            ? Api.GetMyEnrolmentsAsync()
            : Task.FromResult(new ApiResult<List<EnrolmentDto>> { Success = true, Value = new() });
        await Task.WhenAll(eventTask, enrolmentsTask);

        var ev = eventTask.Result;
        if (!ev.Success) return ApiProblem(ev);

        var mine = enrolmentsTask.Result.Success
            ? enrolmentsTask.Result.Value!.Where(e => e.EventId == id).ToDictionary(e => e.CategoryId)
            : new Dictionary<int, EnrolmentDto>();

        return View(new EventDetailsViewModel
        {
            Event = ev.Value!,
            MyEnrolments = mine,
            IsOwner = Me.IsOrganiser && ev.Value!.OrganiserId == Me.UserId
        });
    }
}

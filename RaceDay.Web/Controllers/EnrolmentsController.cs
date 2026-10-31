using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>A Participant's own enrolments: enrol, list, cancel.</summary>
[RaceDayAuthorize(Roles = SessionUser.Participant)]
public class EnrolmentsController : RaceDayController
{
    public EnrolmentsController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    [HttpGet("my-enrolments")]
    public async Task<IActionResult> Index()
    {
        var enrolmentsTask = Api.GetMyEnrolmentsAsync();
        var eventsTask = Api.GetEventsAsync(upcomingOnly: false);
        await Task.WhenAll(enrolmentsTask, eventsTask);

        var enrolments = enrolmentsTask.Result;
        if (!enrolments.Success) return ApiProblem(enrolments);

        // The enrolment only carries the event's name; look up its date, place and type for display.
        var events = eventsTask.Result.Success
            ? eventsTask.Result.Value!.ToDictionary(e => e.EventId)
            : new Dictionary<int, EventDto>();

        var rows = enrolments.Value!
            .Select(e => new MyEnrolmentRow
            {
                Enrolment = e,
                EventDate = events.GetValueOrDefault(e.EventId)?.EventDate,
                EventLocation = events.GetValueOrDefault(e.EventId)?.Location,
                EventType = events.GetValueOrDefault(e.EventId)?.EventType
            })
            .ToList();

        return View(rows);
    }

    /// <summary>Records the enrolment straight away.</summary>
    [HttpPost("enrolments/enrol")]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var result = await Api.EnrolAsync(categoryId);
        if (!result.Success)
        {
            Failure(result.Message);   // e.g. already enrolled, or the category is full
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        Success($"You're in! You're enrolled in {result.Value!.CategoryName} at {result.Value.EventName}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("my-enrolments/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await Api.CancelEnrolmentAsync(id);
        if (result.Success) Success("Your enrolment has been cancelled.");
        else Failure(result.Message);
        return RedirectToAction(nameof(Index));
    }
}

using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Who has enrolled for an event, in which category, and with what status.</summary>
[RaceDayAuthorize(Roles = SessionUser.Organiser)]
public class OrganiserEnrolmentsController : RaceDayController
{
    public OrganiserEnrolmentsController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    /// <summary>The "Enrolments" menu item: first asks which event.</summary>
    [HttpGet("organiser/enrolments")]
    public Task<IActionResult> Picker() =>
        EventPickerAsync("Enrolments", "Choose an event to see who has enrolled.",
            "View enrolments", "bi-people",
            e => Url.Action(nameof(Index), new { eventId = e.EventId })!);

    [HttpGet("organiser/events/{eventId:int}/enrolments")]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, problem) = await LoadOwnedEventAsync(eventId);
        if (problem is not null) return problem;

        var roster = await Api.GetEventEnrolmentsAsync(eventId);
        if (!roster.Success) return ApiProblem(roster);

        return View(new RosterViewModel
        {
            Event = ev!,
            Enrolments = roster.Value!.OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName).ToList()
        });
    }
}

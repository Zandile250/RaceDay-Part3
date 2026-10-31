using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RaceDayAuthorize(Roles = SessionUser.Organiser)]
[Route("organiser")]
public class OrganiserController : RaceDayController
{
    public OrganiserController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    /// <summary>Dashboard: all the Organiser's events, enrolments per event, and upcoming dates.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var events = await Api.GetEventsAsync(upcomingOnly: false);
        if (!events.Success) return ApiProblem(events);

        var mine = events.Value!.Where(e => e.OrganiserId == Me.UserId).OrderBy(e => e.EventDate).ToList();

        // One roster request per event, in parallel; a failed roster just shows 0 rather than breaking the dashboard.
        var summaries = await Task.WhenAll(mine.Select(async e =>
        {
            var roster = await Api.GetEventEnrolmentsAsync(e.EventId);
            return new EventSummary { Event = e, Enrolments = roster.Success ? roster.Value!.Count : 0 };
        }));

        return View(new DashboardViewModel { Events = summaries.ToList() });
    }
}

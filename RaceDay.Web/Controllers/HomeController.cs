using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : RaceDayController
{
    public HomeController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    /// <summary>
    /// "/" depends on who is looking: visitors get the welcome page, Organisers go to their dashboard,
    /// Participants see all upcoming events with filters.
    /// </summary>
    [HttpGet("/")]
    public async Task<IActionResult> Index([FromQuery] EventFilter filter)
    {
        if (!Me.IsAuthenticated) return View("Landing");
        if (Me.IsOrganiser) return RedirectToAction("Index", "Organiser");

        var eventsTask = Api.GetEventsAsync(upcomingOnly: true);
        var enrolmentsTask = Api.GetMyEnrolmentsAsync();
        await Task.WhenAll(eventsTask, enrolmentsTask);

        var events = eventsTask.Result;
        if (!events.Success) return ApiProblem(events);

        var all = events.Value!;
        return View(new HomeViewModel
        {
            Filter = filter,
            TotalUpcoming = all.Count,
            Events = ApplyFilter(all, filter).ToList(),
            EnrolledEventIds = enrolmentsTask.Result.Success
                ? enrolmentsTask.Result.Value!.Select(e => e.EventId).ToHashSet()
                : new HashSet<int>()
        });
    }

    private static IEnumerable<EventDto> ApplyFilter(IEnumerable<EventDto> events, EventFilter f)
    {
        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var term = f.Q.Trim();
            events = events.Where(e =>
                e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (!string.IsNullOrWhiteSpace(f.Type))
            events = events.Where(e => e.EventType.Equals(f.Type, StringComparison.OrdinalIgnoreCase));
        if (f.From is not null) events = events.Where(e => e.EventDate.Date >= f.From.Value.Date);
        if (f.To is not null) events = events.Where(e => e.EventDate.Date <= f.To.Value.Date);
        if (f.MinKm is not null) events = events.Where(e => e.DistanceKm >= f.MinKm);
        if (f.MaxKm is not null) events = events.Where(e => e.DistanceKm <= f.MaxKm);
        return events;
    }

    // ---- friendly pages (no API calls here, so they work even when the API is down) ----

    [AcceptVerbs("GET", "POST")]
    [IgnoreAntiforgeryToken]
    [Route("unavailable")]
    public IActionResult Unavailable()
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return View("Status", new StatusPageViewModel
        {
            Code = 503,
            Title = "We can't reach RaceDay right now",
            Message = "The service that holds your events is not responding. Nothing you did caused this. Please try again in a moment."
        });
    }

    [AcceptVerbs("GET", "POST")]
    [IgnoreAntiforgeryToken]
    [Route("error")]
    public IActionResult Error() => View("Status", new StatusPageViewModel
    {
        Code = 500,
        Title = "Something went wrong",
        Message = "That didn't work, but it wasn't your fault. Please go back and try again."
    });

    [AcceptVerbs("GET", "POST")]
    [IgnoreAntiforgeryToken]
    [Route("status/{code:int}")]
    public IActionResult StatusPage(int code)
    {
        var page = code switch
        {
            404 => new StatusPageViewModel { Code = 404, Title = "Off the course", Message = "We couldn't find that page. It may have moved, or the event may no longer exist." },
            403 => new StatusPageViewModel { Code = 403, Title = "Access denied", Message = "Your account doesn't have access to that page." },
            400 => new StatusPageViewModel { Code = 400, Title = "That request wasn't valid", Message = "Please go back and check what you entered." },
            413 => new StatusPageViewModel { Code = 413, Title = "That file is too large", Message = "Please choose an image of 5 MB or smaller." },
            _ => new StatusPageViewModel { Code = code, Title = "Something went wrong", Message = "Please go back and try again." }
        };
        return View("Status", page);
    }
}

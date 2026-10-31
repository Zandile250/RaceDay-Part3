using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers for every RaceDay page: the API client, the signed-in user and flash messages.</summary>
public abstract class RaceDayController : Controller
{
    protected RaceDayController(IRaceDayApi api, ICurrentUser me)
    {
        Api = api;
        Me = me;
    }

    protected IRaceDayApi Api { get; }
    protected ICurrentUser Me { get; }

    /// <summary>Green message shown at the top of the next page.</summary>
    protected void Success(string message) => TempData["Success"] = message;

    /// <summary>Red message shown at the top of the next page.</summary>
    protected void Failure(string message) => TempData["Error"] = message;

    /// <summary>A page could not load what it needs from the API: show the matching friendly page.</summary>
    protected IActionResult ApiProblem(ApiResult result) => result.StatusCode switch
    {
        403 => RedirectToAction("AccessDenied", "Account"),
        404 => NotFound(),
        >= 500 => RedirectToAction("Unavailable", "Home"),
        _ => StatusCode(result.StatusCode)
    };

    /// <summary>Loads an event and checks it belongs to the signed-in Organiser (the API enforces this too).</summary>
    protected async Task<(EventDetailDto? Event, IActionResult? Problem)> LoadOwnedEventAsync(int eventId)
    {
        var result = await Api.GetEventAsync(eventId);
        if (!result.Success) return (null, ApiProblem(result));
        if (result.Value!.OrganiserId != Me.UserId) return (null, RedirectToAction("AccessDenied", "Account"));
        return (result.Value, null);
    }

    /// <summary>The "which event?" step used by the Categories, Enrolments and Results menu items.</summary>
    protected async Task<IActionResult> EventPickerAsync(string title, string intro, string actionLabel, string icon,
        Func<EventDto, string> linkFor)
    {
        var events = await Api.GetEventsAsync(upcomingOnly: false);
        if (!events.Success) return ApiProblem(events);

        return View("EventPicker", new EventPickerViewModel
        {
            Title = title,
            Intro = intro,
            ActionLabel = actionLabel,
            Icon = icon,
            LinkFor = linkFor,
            Events = events.Value!.Where(e => e.OrganiserId == Me.UserId).OrderBy(e => e.EventDate).ToList()
        });
    }

    protected static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

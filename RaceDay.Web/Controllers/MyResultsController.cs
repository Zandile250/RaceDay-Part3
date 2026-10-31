using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RaceDayAuthorize(Roles = SessionUser.Participant)]
public class MyResultsController : RaceDayController
{
    public MyResultsController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    /// <summary>Personal race history: event, date, category, finish time and "47th of 312 finishers".</summary>
    [HttpGet("my-results")]
    public async Task<IActionResult> Index()
    {
        var mine = await Api.GetMyResultsAsync();
        if (!mine.Success) return ApiProblem(mine);

        // For each event the participant has a result in, fetch its date and its full results list (to count finishers).
        var eventIds = mine.Value!.Select(r => r.EventId).Distinct().ToList();
        var lookups = await Task.WhenAll(eventIds.Select(async id =>
        {
            var ev = Api.GetEventAsync(id);
            var all = Api.GetEventResultsAsync(id);
            await Task.WhenAll(ev, all);
            return (Id: id, Event: ev.Result, Results: all.Result);
        }));
        var byEvent = lookups.ToDictionary(l => l.Id);

        var rows = mine.Value!
            .Select(r =>
            {
                byEvent.TryGetValue(r.EventId, out var info);
                // "Finishers" = everyone in the event whose result status is Finished.
                int? finishers = info.Results is { Success: true }
                    ? info.Results.Value!.Count(x => x.Status.Equals("Finished", StringComparison.OrdinalIgnoreCase))
                    : null;
                return new MyResultRow
                {
                    Result = r,
                    EventDate = info.Event is { Success: true } ? info.Event.Value!.EventDate : null,
                    TotalFinishers = finishers
                };
            })
            .OrderByDescending(r => r.EventDate ?? r.Result.CapturedAt)
            .ToList();

        return View(rows);
    }
}

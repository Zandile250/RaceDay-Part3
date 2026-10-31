using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Capture and publish finish times and positions once an event is over.</summary>
[RaceDayAuthorize(Roles = SessionUser.Organiser)]
public class OrganiserResultsController : RaceDayController
{
    // Exact formats only: a loose parse would read "23:45" as 23 hours 45 minutes.
    private static readonly string[] TimeFormats =
        { @"h\:mm\:ss", @"hh\:mm\:ss", @"h\:mm\:ss\.FFF", @"hh\:mm\:ss\.FFF", @"m\:ss", @"mm\:ss" };

    public OrganiserResultsController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    /// <summary>The "Results" menu item: first asks which event.</summary>
    [HttpGet("organiser/results")]
    public Task<IActionResult> Picker() =>
        EventPickerAsync("Results", "Choose an event to enter or update its results.",
            "Enter results", "bi-stopwatch",
            e => Url.Action(nameof(Index), new { eventId = e.EventId })!);

    [HttpGet("organiser/events/{eventId:int}/results")]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, problem) = await LoadOwnedEventAsync(eventId);
        if (problem is not null) return problem;

        var rosterTask = Api.GetEventEnrolmentsAsync(eventId);
        var resultsTask = Api.GetEventResultsAsync(eventId);
        await Task.WhenAll(rosterTask, resultsTask);

        if (!rosterTask.Result.Success) return ApiProblem(rosterTask.Result);
        if (!resultsTask.Result.Success) return ApiProblem(resultsTask.Result);

        var resultsByEnrolment = resultsTask.Result.Value!.ToDictionary(r => r.EnrolmentId);

        return View(new ResultsPageViewModel
        {
            Event = ev!,
            Rows = rosterTask.Result.Value!
                .OrderBy(e => e.CategoryName).ThenBy(e => e.ParticipantName)
                .Select(e => new ResultRow { Enrolment = e, Result = resultsByEnrolment.GetValueOrDefault(e.EnrolmentId) })
                .ToList()
        });
    }

    /// <summary>Saves (and so publishes) one participant's result: creates it the first time, updates it afterwards.</summary>
    [HttpPost("organiser/events/{eventId:int}/results")]
    public async Task<IActionResult> Save(int eventId, ResultFormViewModel form)
    {
        var back = Url.Action(nameof(Index), new { eventId }) + "#row-" + form.EnrolmentId;

        TimeSpan? finishTime = null;
        if (!string.IsNullOrWhiteSpace(form.FinishTimeText))
        {
            if (!TimeSpan.TryParseExact(form.FinishTimeText.Trim(), TimeFormats, CultureInfo.InvariantCulture, out var parsed))
            {
                Failure("Enter the finish time like 1:23:45 (hours:minutes:seconds), or 23:45 for under an hour.");
                return Redirect(back);
            }
            finishTime = parsed;
        }

        if (!ModelState.IsValid)
        {
            Failure(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrEmpty(m))
                    ?? "Please check the result and try again.");
            return Redirect(back);
        }

        var result = form.ResultId is int resultId
            ? await Api.UpdateResultAsync(resultId, new UpdateResultBody(finishTime, form.Position, form.Status))
            : await Api.CaptureResultAsync(new CaptureResultBody(form.EnrolmentId, finishTime, form.Position, form.Status));

        if (result.Success) Success($"Result saved and published for {result.Value!.ParticipantName}.");
        else Failure(result.Message);

        return Redirect(back);
    }
}

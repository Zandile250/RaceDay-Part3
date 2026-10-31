using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Create, edit, delete events, including the banner image.</summary>
[RaceDayAuthorize(Roles = SessionUser.Organiser)]
[Route("organiser/events")]
public class OrganiserEventsController : RaceDayController
{
    private const long MaxUploadBytes = 6 * 1024 * 1024;   // a little over the API's 5 MB image limit

    public OrganiserEventsController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var events = await Api.GetEventsAsync(upcomingOnly: false);
        if (!events.Success) return ApiProblem(events);

        return View(events.Value!.Where(e => e.OrganiserId == Me.UserId).OrderBy(e => e.EventDate).ToList());
    }

    // ---------------- create ----------------

    [HttpGet("create")]
    public IActionResult Create() => View("Form", new EventFormViewModel
    {
        EventDate = DateTime.Today.AddDays(30).AddHours(7)   // a sensible starting point
    });

    [HttpPost("create")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        CheckBanner(model);
        if (!ModelState.IsValid) return View("Form", model);

        var created = await Api.CreateEventAsync(ToBody(model, imageUrl: null));
        if (!created.Success)
        {
            ModelState.AddApiProblems(created);
            return View("Form", model);
        }

        var ev = created.Value!;
        if (model.Banner is { Length: > 0 })
        {
            var upload = await Api.UploadBannerAsync(ev.EventId, model.Banner);
            if (!upload.Success)
            {
                Failure($"“{ev.EventName}” was created, but its banner couldn't be uploaded: {upload.Message} " +
                        "You can add it again from Edit.");
                return RedirectToAction("Index", "OrganiserCategories", new { eventId = ev.EventId });
            }
        }

        Success($"“{ev.EventName}” has been created. Next, add the categories participants can enter.");
        return RedirectToAction("Index", "OrganiserCategories", new { eventId = ev.EventId });
    }

    // ---------------- edit ----------------

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var (ev, problem) = await LoadOwnedEventAsync(id);
        if (problem is not null) return problem;

        return View("Form", new EventFormViewModel
        {
            EventId = ev!.EventId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            RouteInfo = ev.RouteInfo,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            ExistingImageUrl = ev.ImageUrl
        });
    }

    [HttpPost("{id:int}/edit")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        CheckBanner(model);
        if (!ModelState.IsValid) return View("Form", model);

        // Send the existing banner back so saving the form doesn't wipe it.
        var updated = await Api.UpdateEventAsync(id, ToBody(model, model.ExistingImageUrl));
        if (!updated.Success)
        {
            if (updated.IsForbidden || updated.IsNotFound) return ApiProblem(updated);
            ModelState.AddApiProblems(updated);
            return View("Form", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var upload = await Api.UploadBannerAsync(id, model.Banner);
            if (!upload.Success)
            {
                ModelState.AddModelError(nameof(model.Banner), upload.Message);
                Failure("Your changes were saved, but the new banner couldn't be uploaded.");
                return View("Form", model);
            }
        }

        Success($"“{updated.Value!.EventName}” has been updated.");
        return RedirectToAction(nameof(Index));
    }

    // ---------------- delete ----------------

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (result.Success) Success("The event has been deleted.");
        else Failure(result.Message);   // e.g. participants are already enrolled
        return RedirectToAction(nameof(Index));
    }

    // ---------------- helpers ----------------

    private void CheckBanner(EventFormViewModel model)
    {
        if (model.Banner is { Length: > 5 * 1024 * 1024 })
            ModelState.AddModelError(nameof(model.Banner), "The banner must be 5 MB or smaller.");
    }

    private static EventBody ToBody(EventFormViewModel m, string? imageUrl) => new(
        m.EventName.Trim(),
        NullIfBlank(m.Description),
        m.EventDate!.Value,
        m.Location.Trim(),
        NullIfBlank(m.RouteInfo),
        imageUrl,
        m.DistanceKm!.Value,
        m.EventType);
}

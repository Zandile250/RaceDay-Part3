using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Age or distance categories for each of the Organiser's events.</summary>
[RaceDayAuthorize(Roles = SessionUser.Organiser)]
public class OrganiserCategoriesController : RaceDayController
{
    public OrganiserCategoriesController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    /// <summary>The "Categories" menu item: first asks which event.</summary>
    [HttpGet("organiser/categories")]
    public Task<IActionResult> Picker() =>
        EventPickerAsync("Categories", "Choose an event to manage its categories.",
            "Manage categories", "bi-tags",
            e => Url.Action(nameof(Index), new { eventId = e.EventId })!);

    [HttpGet("organiser/events/{eventId:int}/categories")]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, problem) = await LoadOwnedEventAsync(eventId);
        if (problem is not null) return problem;

        return View(new CategoriesPageViewModel { Event = ev!, Form = new CategoryFormViewModel { EventId = ev!.EventId } });
    }

    [HttpPost("organiser/events/{eventId:int}/categories")]
    public async Task<IActionResult> Create(int eventId, CategoryFormViewModel form)
    {
        form.EventId = eventId;
        if (!ModelState.IsValid) return await ReshowAsync(eventId, form);

        var result = await Api.CreateCategoryAsync(eventId, ToBody(form));
        if (!result.Success)
        {
            if (result.IsForbidden || result.IsNotFound) return ApiProblem(result);
            ModelState.AddApiProblems(result);
            return await ReshowAsync(eventId, form);
        }

        Success($"Category “{result.Value!.CategoryName}” added.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpGet("organiser/events/{eventId:int}/categories/{categoryId:int}/edit")]
    public async Task<IActionResult> Edit(int eventId, int categoryId)
    {
        var (ev, problem) = await LoadOwnedEventAsync(eventId);
        if (problem is not null) return problem;

        var cat = ev!.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (cat is null) return NotFound();

        ViewData["EventName"] = ev.EventName;
        return View(new CategoryFormViewModel
        {
            EventId = eventId,
            CategoryId = categoryId,
            CategoryName = cat.CategoryName,
            DistanceKm = cat.DistanceKm,
            Price = cat.Price,
            MaxParticipants = cat.MaxParticipants
        });
    }

    [HttpPost("organiser/events/{eventId:int}/categories/{categoryId:int}/edit")]
    public async Task<IActionResult> Edit(int eventId, int categoryId, CategoryFormViewModel form)
    {
        form.EventId = eventId;
        form.CategoryId = categoryId;

        if (ModelState.IsValid)
        {
            var result = await Api.UpdateCategoryAsync(categoryId, ToBody(form));
            if (result.Success)
            {
                Success($"Category “{result.Value!.CategoryName}” updated.");
                return RedirectToAction(nameof(Index), new { eventId });
            }
            if (result.IsForbidden || result.IsNotFound) return ApiProblem(result);
            ModelState.AddApiProblems(result);
        }

        var ev = await Api.GetEventAsync(eventId);
        ViewData["EventName"] = ev.Success ? ev.Value!.EventName : "";
        return View(form);
    }

    [HttpPost("organiser/events/{eventId:int}/categories/{categoryId:int}/delete")]
    public async Task<IActionResult> Delete(int eventId, int categoryId)
    {
        var result = await Api.DeleteCategoryAsync(categoryId);
        if (result.Success) Success("The category has been removed.");
        else Failure(result.Message);   // e.g. participants are already enrolled in it
        return RedirectToAction(nameof(Index), new { eventId });
    }

    // Invalid add-category form: show the page again with what the user typed.
    private async Task<IActionResult> ReshowAsync(int eventId, CategoryFormViewModel form)
    {
        var (ev, problem) = await LoadOwnedEventAsync(eventId);
        if (problem is not null) return problem;
        return View(nameof(Index), new CategoriesPageViewModel { Event = ev!, Form = form });
    }

    private static CategoryBody ToBody(CategoryFormViewModel f) =>
        new(f.CategoryName.Trim(), f.DistanceKm!.Value, f.Price!.Value, f.MaxParticipants!.Value);
}

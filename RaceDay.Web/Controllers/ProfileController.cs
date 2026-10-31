using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RaceDayAuthorize]
[Route("profile")]
public class ProfileController : RaceDayController
{
    private const long MaxUploadBytes = 6 * 1024 * 1024;

    public ProfileController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var me = await Api.GetMeAsync();
        if (!me.Success) return ApiProblem(me);
        return View(ToViewModel(me.Value!));
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return await ReshowAsync(model);

        var result = await Api.UpdateMeAsync(new UpdateProfileBody(model.FullName.Trim(), NullIfBlank(model.PhoneNumber)));
        if (!result.Success)
        {
            ModelState.AddApiProblems(result);
            return await ReshowAsync(model);
        }

        HttpContext.Session.SetUser(Me.User! with { FullName = result.Value!.FullName });
        Success("Your details have been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("picture")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            Failure("Please choose a picture to upload.");
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.UploadProfilePictureAsync(picture);
        if (!result.Success)
        {
            Failure(result.Message);
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetUser(Me.User! with { ProfilePictureUrl = result.Value!.ImageUrl });
        Success("Your profile picture has been updated.");
        return RedirectToAction(nameof(Index));
    }

    // Invalid form: show it again with the user's input, refreshing the read-only details beside it.
    private async Task<IActionResult> ReshowAsync(ProfileViewModel posted)
    {
        var me = await Api.GetMeAsync();
        if (!me.Success) return ApiProblem(me);

        var fresh = ToViewModel(me.Value!);
        posted.Email = fresh.Email;
        posted.Role = fresh.Role;
        posted.ProfilePictureUrl = fresh.ProfilePictureUrl;
        posted.MemberSince = fresh.MemberSince;
        return View(nameof(Index), posted);
    }

    private static ProfileViewModel ToViewModel(UserDto u) => new()
    {
        FullName = u.FullName,
        PhoneNumber = u.PhoneNumber,
        Email = u.Email,
        Role = u.Role,
        ProfilePictureUrl = u.ProfilePictureUrl,
        MemberSince = u.CreatedAt
    };
}

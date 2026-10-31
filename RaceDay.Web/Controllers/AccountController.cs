using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Route("account")]
public class AccountController : RaceDayController
{
    public AccountController(IRaceDayApi api, ICurrentUser me) : base(api, me) { }

    // ---------------- sign in ----------------

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null, bool required = false, bool expired = false)
    {
        if (Me.IsAuthenticated) return RedirectToAction("Index", "Home");

        if (expired) ViewData["Notice"] = "Your session has ended, so please sign in again.";
        else if (required) ViewData["Notice"] = "Please sign in to continue.";

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await SignInAsync(model.Email, model.Password);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.StatusCode == 401
                ? "That email and password don't match. Please try again."
                : result.Message);
            return View(model);
        }

        return RedirectAfterSignIn(model.ReturnUrl);
    }

    // ---------------- register ----------------

    [HttpGet("register")]
    public IActionResult Register()
    {
        if (Me.IsAuthenticated) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == SessionUser.Organiser && string.IsNullOrWhiteSpace(model.OrganiserInviteCode))
            ModelState.AddModelError(nameof(model.OrganiserInviteCode), "Organisers need an invite code.");

        if (!ModelState.IsValid) return View(model);

        var created = await Api.RegisterAsync(new RegisterBody(
            model.FullName.Trim(), model.Email.Trim(), model.Password, NullIfBlank(model.PhoneNumber),
            model.Role, model.Role == SessionUser.Organiser ? model.OrganiserInviteCode!.Trim() : null));

        if (!created.Success)
        {
            ModelState.AddApiProblems(created);
            return View(model);
        }

        // Account created: sign them straight in.
        var signedIn = await SignInAsync(model.Email, model.Password);
        if (!signedIn.Success)
        {
            Success("Your account is ready. Please sign in.");
            return RedirectToAction(nameof(Login));
        }

        Success($"Welcome to RaceDay, {Me.User!.FirstName}!");
        return RedirectToAction("Index", "Home");
    }

    // ---------------- sign out ----------------

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Me.IsAuthenticated) await Api.LogoutAsync();   // ends the API session too
        HttpContext.Session.Clear();                         // and ours

        Success("You've been signed out. See you at the start line!");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    // ---------------- helpers ----------------

    private async Task<ApiResult<UserDto>> SignInAsync(string email, string password)
    {
        HttpContext.Session.Clear();   // never carry anything over from a previous user
        var result = await Api.LoginAsync(email.Trim(), password);
        if (result.Success)
        {
            var u = result.Value!;
            HttpContext.Session.SetUser(new SessionUser(u.UserId, u.FullName, u.Email, u.Role, u.ProfilePictureUrl));
        }
        return result;
    }

    private IActionResult RedirectAfterSignIn(string? returnUrl)
    {
        // Only go back to a local page this role may open; otherwise land on the role's home page.
        var allowed = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) &&
                      !(returnUrl.StartsWith("/organiser", StringComparison.OrdinalIgnoreCase) && !Me.IsOrganiser);

        return allowed ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
    }
}

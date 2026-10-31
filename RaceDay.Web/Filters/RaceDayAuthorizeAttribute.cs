using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects pages using the MVC session: not signed in -> sign-in page; wrong role -> access denied.
/// Usage: [RaceDayAuthorize] for any signed-in user, [RaceDayAuthorize(Roles = "Organiser")] for one role.
/// (The API makes the same checks again on every call - this just keeps people out of pages they cannot use.)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RaceDayAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Roles { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.Session.GetUser();

        if (user is null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = HttpMethods.IsGet(request.Method) ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl, required = true });
            return;
        }

        if (Roles is not null)
        {
            var allowed = Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (!allowed.Contains(user.Role))
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

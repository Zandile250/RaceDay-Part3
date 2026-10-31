using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>Turns the two "the API is the problem" situations into friendly pages instead of error screens.</summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ApiSessionExpiredException:
                // The API forgot this user (session timed out or the API restarted): sign in again.
                context.HttpContext.Session.Clear();
                context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
                context.ExceptionHandled = true;
                break;

            case ApiUnavailableException:
                context.Result = new RedirectToActionResult("Unavailable", "Home", null);
                context.ExceptionHandled = true;
                break;
        }
    }
}

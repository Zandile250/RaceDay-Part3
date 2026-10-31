using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RaceDay.Web.Services;

public static class ModelStateExtensions
{
    /// <summary>
    /// Shows the API's answer on the form: field messages next to their fields, anything else in the summary.
    /// Technical keys (e.g. JSON path errors) are never shown, only the friendly general message.
    /// </summary>
    public static void AddApiProblems(this ModelStateDictionary modelState, ApiResult result)
    {
        var needGeneral = result.Errors.Count == 0;

        foreach (var (key, messages) in result.Errors)
        {
            var field = key.Contains('$') ? null : key.Split('.').Last();
            if (field is not null && modelState.ContainsKey(field))
            {
                foreach (var message in messages) modelState.AddModelError(field, message);
            }
            else
            {
                needGeneral = true;
            }
        }

        if (needGeneral) modelState.AddModelError("", result.Message);
    }
}

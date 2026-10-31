using System.Globalization;

namespace RaceDay.Web.Services;

/// <summary>Small presentation helpers shared by the views (formatting, colours, icons, image URLs).</summary>
public static class UiHelpers
{
    public static string CurrencySymbol { get; set; } = "R";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Money(decimal amount) =>
        amount == 0 ? "Free" : $"{CurrencySymbol}{amount.ToString("N2", Inv)}";

    public static string DateLong(DateTime d) => d.ToString("ddd, d MMM yyyy", Inv);
    public static string DateShort(DateTime d) => d.ToString("d MMM yyyy", Inv);
    public static string TimeOfDay(DateTime d) => d.ToString("HH:mm", Inv);

    public static string Km(decimal km) => km.ToString("0.##", Inv) + " km";

    /// <summary>1:23:45 (hours only when needed).</summary>
    public static string FinishTime(TimeSpan? t) =>
        t is null ? "—" : t.Value.TotalHours >= 1
            ? $"{(int)t.Value.TotalHours}:{t.Value.Minutes:00}:{t.Value.Seconds:00}"
            : $"{t.Value.Minutes}:{t.Value.Seconds:00}";

    /// <summary>Value for the finish-time input box: 1:23:45, or empty.</summary>
    public static string FinishTimeInput(TimeSpan? t) =>
        t is null ? "" : $"{(int)t.Value.TotalHours}:{t.Value.Minutes:00}:{t.Value.Seconds:00}";

    /// <summary>1st, 2nd, 3rd, 47th ...</summary>
    public static string Ordinal(int n)
    {
        var lastTwo = n % 100;
        if (lastTwo is >= 11 and <= 13) return n + "th";
        return n + (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
    }

    /// <summary>"in 12 days", "tomorrow", "today", "3 days ago".</summary>
    public static string Countdown(DateTime date)
    {
        var days = (date.Date - DateTime.Now.Date).Days;
        return days switch
        {
            0 => "today",
            1 => "tomorrow",
            -1 => "yesterday",
            > 1 => $"in {days} days",
            _ => $"{-days} days ago"
        };
    }

    /// <summary>CSS class for a colour-coded status pill (enrolment and result statuses).</summary>
    public static string StatusClass(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "confirmed" or "finished" => "status-confirmed",
        "pending" or "dnf" or "dns" => "status-pending",
        "cancelled" or "canceled" or "disqualified" or "dq" => "status-cancelled",
        _ => "status-neutral"
    };

    public static string StatusIcon(string? status) => (status ?? "").Trim().ToLowerInvariant() switch
    {
        "confirmed" or "finished" => "bi-check-circle-fill",
        "pending" or "dnf" or "dns" => "bi-hourglass-split",
        "cancelled" or "canceled" or "disqualified" or "dq" => "bi-x-circle-fill",
        _ => "bi-circle"
    };

    public static string TypeIcon(string? eventType) => eventType switch
    {
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-lightning-charge-fill"
    };

    public static string TypeClass(string? eventType) => "type-" + (eventType ?? "run").ToLowerInvariant();

    /// <summary>
    /// Turns the ImageUrl stored by the API into something a browser can load.
    /// Images uploaded through the API live in Azure Blob Storage and are fetched through this site's /media route
    /// (so the browser never talks to Azure, and neither does MVC); external links are used as they are.
    /// </summary>
    public static string? ImageSrc(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl)) return null;
        const string apiPrefix = "api/files/";
        if (storedUrl.StartsWith(apiPrefix, StringComparison.Ordinal)) return "/media/" + storedUrl[apiPrefix.Length..];
        if (Uri.TryCreate(storedUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") return storedUrl;
        return null;
    }
}

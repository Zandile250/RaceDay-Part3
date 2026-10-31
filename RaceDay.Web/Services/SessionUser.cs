using System.Text.Json;

namespace RaceDay.Web.Services;

/// <summary>What the MVC application remembers about the signed-in user between requests.</summary>
public sealed record SessionUser(int UserId, string FullName, string Email, string Role, string? ProfilePictureUrl)
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public bool IsOrganiser => Role == Organiser;
    public bool IsParticipant => Role == Participant;

    public string FirstName => FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? FullName;

    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }
    }
}

/// <summary>Keys and helpers for the server-side MVC session.</summary>
public static class SessionStore
{
    private const string UserKey = "raceday.user";
    private const string ApiCookieKey = "raceday.api-cookie";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static SessionUser? GetUser(this ISession session)
    {
        var json = session.GetString(UserKey);
        return json is null ? null : JsonSerializer.Deserialize<SessionUser>(json, Json);
    }

    public static void SetUser(this ISession session, SessionUser user) =>
        session.SetString(UserKey, JsonSerializer.Serialize(user, Json));

    /// <summary>The "name=value" session cookie issued by the API, replayed on every API call for this user.</summary>
    public static string? GetApiCookie(this ISession session) => session.GetString(ApiCookieKey);

    public static void SetApiCookie(this ISession session, string cookie) => session.SetString(ApiCookieKey, cookie);
}

public interface ICurrentUser
{
    SessionUser? User { get; }
    bool IsAuthenticated { get; }
    bool IsOrganiser { get; }
    bool IsParticipant { get; }
    int UserId { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public SessionUser? User => _accessor.HttpContext?.Session.GetUser();
    public bool IsAuthenticated => User is not null;
    public bool IsOrganiser => User?.IsOrganiser == true;
    public bool IsParticipant => User?.IsParticipant == true;
    public int UserId => User?.UserId ?? 0;
}

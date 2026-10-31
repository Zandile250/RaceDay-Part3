namespace RaceDay.Web.Services;

/// <summary>
/// The outcome of one call to the RaceDay API. Expected failures (validation, not found, conflicts)
/// come back as a result so pages can show a friendly message; only an expired session or an
/// unreachable API is thrown (see <see cref="ApiSessionExpiredException"/> and <see cref="ApiUnavailableException"/>).
/// </summary>
public class ApiResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }

    /// <summary>A message that is safe to show to the user.</summary>
    public string Message { get; init; } = "";

    /// <summary>Field-level validation messages from the API, keyed by property name.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();

    public bool IsNotFound => StatusCode == 404;
    public bool IsForbidden => StatusCode == 403;
    public bool IsConflict => StatusCode == 409;
}

public class ApiResult<T> : ApiResult
{
    public T? Value { get; init; }
}

/// <summary>The API no longer recognises this user's session (it expired, or the API restarted).</summary>
public class ApiSessionExpiredException : Exception
{
    public ApiSessionExpiredException() : base("The API session has expired.") { }
}

/// <summary>The API could not be reached (down, timing out, network failure).</summary>
public class ApiUnavailableException : Exception
{
    public ApiUnavailableException(Exception inner) : base("The RaceDay API is unavailable.", inner) { }
}

/// <summary>A downloaded image.</summary>
public sealed record ApiFile(byte[] Bytes, string ContentType);

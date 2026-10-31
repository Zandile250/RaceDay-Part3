using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>
/// Typed HttpClient for the RaceDay API.
///
/// The API authenticates with its own session cookie. At login this class captures that cookie, keeps it
/// in the user's MVC session, and attaches it to every later call, so the API sees the same logged-in user
/// as the MVC application does. (The handler has UseCookies = false, so cookies from different users can
/// never be mixed up in a shared connection pool.)
/// </summary>
public sealed class RaceDayApi : IRaceDayApi
{
    private const string ApiSessionCookieName = ".AspNetCore.Session";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApi> _log;

    // Read once, up front: pages make several API calls in parallel and the session is not meant to be read concurrently.
    private string? _apiCookie;

    public RaceDayApi(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApi> log)
    {
        _http = http;
        _accessor = accessor;
        _log = log;
        _apiCookie = accessor.HttpContext?.Session.GetApiCookie();
    }

    private ISession? Session => _accessor.HttpContext?.Session;
    private CancellationToken Aborted => _accessor.HttpContext?.RequestAborted ?? CancellationToken.None;

    // ---------------- authentication ----------------

    public Task<ApiResult<UserDto>> RegisterAsync(RegisterBody body) =>
        SendAsync<UserDto>(JsonRequest(HttpMethod.Post, "api/auth/register", body), expireOn401: false);

    public async Task<ApiResult<UserDto>> LoginAsync(string email, string password) =>
        await SendAsync<UserDto>(JsonRequest(HttpMethod.Post, "api/auth/login", new LoginBody(email, password)),
            expireOn401: false, captureSessionCookie: true);

    public async Task<ApiResult> LogoutAsync()
    {
        try
        {
            return await SendAsync(new HttpRequestMessage(HttpMethod.Post, "api/auth/logout"), expireOn401: false);
        }
        catch (ApiUnavailableException)
        {
            // Signing out must always work locally, even if the API is down.
            return new ApiResult { Success = true, StatusCode = 204 };
        }
    }

    // ---------------- profile ----------------

    public Task<ApiResult<UserDto>> GetMeAsync() => GetAsync<UserDto>("api/users/me");

    public Task<ApiResult<UserDto>> UpdateMeAsync(UpdateProfileBody body) =>
        SendAsync<UserDto>(JsonRequest(HttpMethod.Put, "api/users/me", body));

    public Task<ApiResult<ImageUploadDto>> UploadProfilePictureAsync(IFormFile file) =>
        SendAsync<ImageUploadDto>(FileRequest("api/users/me/picture", file));

    // ---------------- events and categories ----------------

    public Task<ApiResult<List<EventDto>>> GetEventsAsync(bool upcomingOnly) =>
        GetAsync<List<EventDto>>(upcomingOnly ? "api/events?upcoming=true" : "api/events");

    public Task<ApiResult<EventDetailDto>> GetEventAsync(int eventId) =>
        GetAsync<EventDetailDto>($"api/events/{eventId}");

    public Task<ApiResult<EventDto>> CreateEventAsync(EventBody body) =>
        SendAsync<EventDto>(JsonRequest(HttpMethod.Post, "api/events", body));

    public Task<ApiResult<EventDto>> UpdateEventAsync(int eventId, EventBody body) =>
        SendAsync<EventDto>(JsonRequest(HttpMethod.Put, $"api/events/{eventId}", body));

    public Task<ApiResult> DeleteEventAsync(int eventId) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"api/events/{eventId}"));

    public Task<ApiResult<ImageUploadDto>> UploadBannerAsync(int eventId, IFormFile file) =>
        SendAsync<ImageUploadDto>(FileRequest($"api/events/{eventId}/banner", file));

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryBody body) =>
        SendAsync<CategoryDto>(JsonRequest(HttpMethod.Post, $"api/events/{eventId}/categories", body));

    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, CategoryBody body) =>
        SendAsync<CategoryDto>(JsonRequest(HttpMethod.Put, $"api/categories/{categoryId}", body));

    public Task<ApiResult> DeleteCategoryAsync(int categoryId) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"api/categories/{categoryId}"));

    // ---------------- enrolments ----------------

    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId) =>
        SendAsync<EnrolmentDto>(JsonRequest(HttpMethod.Post, "api/enrolments", new EnrolBody(categoryId)));

    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync() =>
        GetAsync<List<EnrolmentDto>>("api/enrolments/me");

    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");

    public Task<ApiResult> CancelEnrolmentAsync(int enrolmentId) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"api/enrolments/{enrolmentId}"));

    // ---------------- results ----------------

    public Task<ApiResult<ResultDto>> CaptureResultAsync(CaptureResultBody body) =>
        SendAsync<ResultDto>(JsonRequest(HttpMethod.Post, "api/results", body));

    public Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, UpdateResultBody body) =>
        SendAsync<ResultDto>(JsonRequest(HttpMethod.Put, $"api/results/{resultId}", body));

    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync() =>
        GetAsync<List<ResultDto>>("api/results/me");

    public Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId) =>
        GetAsync<List<ResultDto>>($"api/events/{eventId}/results");

    // ---------------- images ----------------

    public async Task<ApiFile?> GetFileAsync(string blobName)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/files/" + blobName);
            using var response = await _http.SendAsync(request, Aborted);
            if (!response.IsSuccessStatusCode) return null;

            var bytes = await response.Content.ReadAsByteArrayAsync(Aborted);
            return new ApiFile(bytes, response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning(ex, "Could not load image {Blob} from the API", blobName);
            return null;
        }
    }

    // ---------------- plumbing ----------------

    private Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, url));

    private static HttpRequestMessage JsonRequest(HttpMethod method, string url, object body) =>
        new(method, url) { Content = JsonContent.Create(body, body.GetType(), options: JsonOptions) };

    private static HttpRequestMessage FileRequest(string url, IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        return new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
    }

    private async Task<ApiResult> SendAsync(HttpRequestMessage request, bool expireOn401 = true) =>
        await SendCoreAsync<object>(request, expireOn401, readBody: false, captureSessionCookie: false);

    private Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, bool expireOn401 = true,
        bool captureSessionCookie = false) =>
        SendCoreAsync<T>(request, expireOn401, readBody: true, captureSessionCookie);

    private async Task<ApiResult<T>> SendCoreAsync<T>(HttpRequestMessage request, bool expireOn401,
        bool readBody, bool captureSessionCookie)
    {
        using (request)
        {
            // A login always starts a brand-new API session, so it must not replay an old cookie.
            if (_apiCookie is not null && !captureSessionCookie)
                request.Headers.TryAddWithoutValidation("Cookie", _apiCookie);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, Aborted);
            }
            catch (Exception ex) when (ex is HttpRequestException ||
                                       (ex is TaskCanceledException && !Aborted.IsCancellationRequested))
            {
                _log.LogError(ex, "RaceDay API unreachable: {Method} {Url}", request.Method, request.RequestUri);
                throw new ApiUnavailableException(ex);
            }

            using (response)
            {
                var status = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    if (captureSessionCookie) StoreSessionCookie(response);

                    T? value = default;
                    if (readBody && response.Content.Headers.ContentLength != 0)
                    {
                        try
                        {
                            value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, Aborted);
                        }
                        catch (JsonException ex)
                        {
                            _log.LogError(ex, "Unexpected response shape from {Url}", request.RequestUri);
                            throw new ApiUnavailableException(ex);
                        }
                    }
                    return new ApiResult<T> { Success = true, StatusCode = status, Value = value };
                }

                if (status == 401 && expireOn401) throw new ApiSessionExpiredException();

                var body = await response.Content.ReadAsStringAsync(Aborted);
                var (message, errors) = ParseError(body);
                if (status >= 500) _log.LogError("RaceDay API returned {Status} for {Url}: {Body}", status, request.RequestUri, body);

                return new ApiResult<T>
                {
                    Success = false,
                    StatusCode = status,
                    Message = string.IsNullOrWhiteSpace(message) ? DefaultMessage(status) : message,
                    Errors = errors
                };
            }
        }
    }

    /// <summary>Keeps the API's session cookie so later calls are made as this user.</summary>
    private void StoreSessionCookie(HttpResponseMessage response)
    {
        if (Session is null || !response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        foreach (var header in values)
        {
            if (!header.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal)) continue;
            var end = header.IndexOf(';');
            _apiCookie = end < 0 ? header : header[..end];
            Session.SetApiCookie(_apiCookie);
            return;
        }
    }

    private static (string? Message, Dictionary<string, string[]> Errors) ParseError(string body)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(body)) return (null, errors);

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return (null, errors);

            string? message = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString() : null;

            if (root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in e.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array) continue;
                    errors[property.Name] = property.Value.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()!)
                        .ToArray();
                }
            }
            return (message, errors);
        }
        catch (JsonException)
        {
            return (null, errors);
        }
    }

    private static string DefaultMessage(int status) => status switch
    {
        400 => "Please check the details you entered and try again.",
        401 => "Please sign in to continue.",
        403 => "You don't have permission to do that.",
        404 => "We couldn't find what you were looking for.",
        409 => "That can't be done right now. Please refresh the page and try again.",
        >= 500 => "Something went wrong on our side. Please try again in a moment.",
        _ => "Something went wrong. Please try again."
    };
}

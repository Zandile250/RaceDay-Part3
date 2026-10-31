using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>Everything the MVC application needs from the Part 2 API. No other class makes HTTP calls to it.</summary>
public interface IRaceDayApi
{
    // Authentication
    Task<ApiResult<UserDto>> RegisterAsync(RegisterBody body);
    Task<ApiResult<UserDto>> LoginAsync(string email, string password);
    Task<ApiResult> LogoutAsync();

    // Profile
    Task<ApiResult<UserDto>> GetMeAsync();
    Task<ApiResult<UserDto>> UpdateMeAsync(UpdateProfileBody body);
    Task<ApiResult<ImageUploadDto>> UploadProfilePictureAsync(IFormFile file);

    // Events and categories
    Task<ApiResult<List<EventDto>>> GetEventsAsync(bool upcomingOnly);
    Task<ApiResult<EventDetailDto>> GetEventAsync(int eventId);
    Task<ApiResult<EventDto>> CreateEventAsync(EventBody body);
    Task<ApiResult<EventDto>> UpdateEventAsync(int eventId, EventBody body);
    Task<ApiResult> DeleteEventAsync(int eventId);
    Task<ApiResult<ImageUploadDto>> UploadBannerAsync(int eventId, IFormFile file);
    Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryBody body);
    Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, CategoryBody body);
    Task<ApiResult> DeleteCategoryAsync(int categoryId);

    // Enrolments
    Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId);
    Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync();
    Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult> CancelEnrolmentAsync(int enrolmentId);

    // Results
    Task<ApiResult<ResultDto>> CaptureResultAsync(CaptureResultBody body);
    Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, UpdateResultBody body);
    Task<ApiResult<List<ResultDto>>> GetMyResultsAsync();
    Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId);

    // Stored images (the API reads them from Azure Blob Storage)
    Task<ApiFile?> GetFileAsync(string blobName);
}

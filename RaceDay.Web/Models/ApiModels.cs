namespace RaceDay.Web.Models;

// Shapes of the JSON the RaceDay API (Part 2) sends and receives.
// The MVC project only moves these around; all rules and data access stay in the API.

// ---- responses ----
public record UserDto(int UserId, string FullName, string Email, string? PhoneNumber,
    string Role, DateTime CreatedAt, string? ProfilePictureUrl);

public record EventDto(int EventId, int OrganiserId, string EventName, string? Description,
    DateTime EventDate, string Location, string? RouteInfo, string? ImageUrl,
    decimal DistanceKm, string EventType, DateTime CreatedAt);

public record EventDetailDto(int EventId, int OrganiserId, string EventName, string? Description,
    DateTime EventDate, string Location, string? RouteInfo, string? ImageUrl,
    decimal DistanceKm, string EventType, DateTime CreatedAt, List<CategoryDto> Categories);

public record CategoryDto(int CategoryId, int EventId, string CategoryName,
    decimal DistanceKm, decimal Price, int MaxParticipants);

public record EnrolmentDto(int EnrolmentId, int ParticipantId, string ParticipantName,
    int CategoryId, string CategoryName, int EventId, string EventName,
    DateTime EnrolmentDate, string Status);

public record ResultDto(int ResultId, int EnrolmentId, int EventId, string EventName,
    string ParticipantName, string CategoryName, TimeSpan? FinishTime, int? Position,
    string Status, DateTime CapturedAt);

public record ImageUploadDto(string ImageUrl);

// ---- requests ----
public record RegisterBody(string FullName, string Email, string Password, string? PhoneNumber,
    string Role, string? OrganiserInviteCode);

public record LoginBody(string Email, string Password);

public record UpdateProfileBody(string FullName, string? PhoneNumber);

public record EventBody(string EventName, string? Description, DateTime EventDate, string Location,
    string? RouteInfo, string? ImageUrl, decimal DistanceKm, string EventType);

public record CategoryBody(string CategoryName, decimal DistanceKm, decimal Price, int MaxParticipants);

public record EnrolBody(int CategoryId);

public record CaptureResultBody(int EnrolmentId, TimeSpan? FinishTime, int? Position, string Status);

public record UpdateResultBody(TimeSpan? FinishTime, int? Position, string Status);

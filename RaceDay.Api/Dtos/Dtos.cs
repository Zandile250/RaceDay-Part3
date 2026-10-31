using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

// ---- Authentication ----
public class RegisterRequest
{
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Phone, MaxLength(20)] public string? PhoneNumber { get; set; }
    [Required] public UserRole Role { get; set; }
    /// <summary>Required only when registering as an Organiser.</summary>
    public string? OrganiserInviteCode { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public record UserResponse(int UserId, string FullName, string Email, string? PhoneNumber,
    string Role, DateTime CreatedAt, string? ProfilePictureUrl = null);

/// <summary>Returned after an image upload: the relative URL now stored for the event or user.</summary>
public record ImageUploadResponse(string ImageUrl);

public class UpdateProfileRequest
{
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Phone, MaxLength(20)] public string? PhoneNumber { get; set; }
}

// ---- Events ----
public class EventRequest
{
    [Required, MaxLength(150)] public string EventName { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    [Required] public DateTime EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = "";
    [MaxLength(500)] public string? RouteInfo { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    [Range(0.1, 999.99)] public decimal DistanceKm { get; set; }
    [Required] public EventType EventType { get; set; }
}

public record EventResponse(int EventId, int OrganiserId, string EventName, string? Description,
    DateTime EventDate, string Location, string? RouteInfo, string? ImageUrl,
    decimal DistanceKm, EventType EventType, DateTime CreatedAt);

public record EventDetailResponse(int EventId, int OrganiserId, string EventName, string? Description,
    DateTime EventDate, string Location, string? RouteInfo, string? ImageUrl,
    decimal DistanceKm, EventType EventType, DateTime CreatedAt, List<CategoryResponse> Categories);

// ---- Categories ----
public class CategoryRequest
{
    [Required, MaxLength(100)] public string CategoryName { get; set; } = "";
    [Range(0.1, 999.99)] public decimal DistanceKm { get; set; }
    [Range(0, 999999.99)] public decimal Price { get; set; }
    [Range(0, 1000000)] public int MaxParticipants { get; set; }
}

public record CategoryResponse(int CategoryId, int EventId, string CategoryName,
    decimal DistanceKm, decimal Price, int MaxParticipants);

// ---- Enrolments ----
public class EnrolRequest
{
    [Required] public int CategoryId { get; set; }
}

public record EnrolmentResponse(int EnrolmentId, int ParticipantId, string ParticipantName,
    int CategoryId, string CategoryName, int EventId, string EventName,
    DateTime EnrolmentDate, string Status);

// ---- Results ----
public class CaptureResultRequest
{
    [Required] public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    [Range(1, 1000000)] public int? Position { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Finished";
}

public class UpdateResultRequest
{
    public TimeSpan? FinishTime { get; set; }
    [Range(1, 1000000)] public int? Position { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Finished";
}

public record ResultResponse(int ResultId, int EnrolmentId, int EventId, string EventName,
    string ParticipantName, string CategoryName, TimeSpan? FinishTime, int? Position,
    string Status, DateTime CapturedAt);

using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

// View models carry what a page shows or collects. The attributes only give instant, friendly feedback on forms;
// the API remains the authority and its answers are shown whenever it disagrees.

// ---------------- account ----------------

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Enter your email address."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Choose a password."), DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Phone(ErrorMessage = "Enter a valid phone number."), StringLength(20)]
    [Display(Name = "Phone number (optional)")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Choose how you will use RaceDay.")]
    public string Role { get; set; } = "Participant";

    [Display(Name = "Organiser invite code")]
    public string? OrganiserInviteCode { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Phone(ErrorMessage = "Enter a valid phone number."), StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    // Read-only details shown beside the form
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
    public DateTime MemberSince { get; set; }
}

// ---------------- participant ----------------

public class EventFilter
{
    [Display(Name = "Search")] public string? Q { get; set; }
    [Display(Name = "Type")] public string? Type { get; set; }
    [Display(Name = "From")] public DateTime? From { get; set; }
    [Display(Name = "To")] public DateTime? To { get; set; }
    [Display(Name = "Min km")] public decimal? MinKm { get; set; }
    [Display(Name = "Max km")] public decimal? MaxKm { get; set; }

    public bool IsActive => !string.IsNullOrWhiteSpace(Q) || !string.IsNullOrWhiteSpace(Type) ||
                            From is not null || To is not null || MinKm is not null || MaxKm is not null;
}

public class HomeViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventFilter Filter { get; set; } = new();
    public int TotalUpcoming { get; set; }
    public HashSet<int> EnrolledEventIds { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public bool IsPast => Event.EventDate < DateTime.Now;

    /// <summary>The signed-in participant's enrolments in this event, keyed by category id.</summary>
    public Dictionary<int, EnrolmentDto> MyEnrolments { get; set; } = new();

    public bool IsOwner { get; set; }
}

public class MyEnrolmentRow
{
    public EnrolmentDto Enrolment { get; set; } = null!;
    public DateTime? EventDate { get; set; }
    public string? EventLocation { get; set; }
    public string? EventType { get; set; }
}

public class MyResultRow
{
    public ResultDto Result { get; set; } = null!;
    public DateTime? EventDate { get; set; }
    public int? TotalFinishers { get; set; }
}

// ---------------- organiser ----------------

public class EventSummary
{
    public EventDto Event { get; set; } = null!;
    public int Enrolments { get; set; }
    public bool IsUpcoming => Event.EventDate >= DateTime.Now;
}

public class DashboardViewModel
{
    public List<EventSummary> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e => e.IsUpcoming);
    public int TotalEnrolments => Events.Sum(e => e.Enrolments);
    public EventSummary? NextEvent => Events.Where(e => e.IsUpcoming).OrderBy(e => e.Event.EventDate).FirstOrDefault();
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Give the event a name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Keep the description under 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose the date and start time."), DataType(DataType.DateTime)]
    [Display(Name = "Date and start time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Enter the location."), StringLength(150)]
    public string Location { get; set; } = "";

    [StringLength(500, ErrorMessage = "Keep the route information under 500 characters.")]
    [Display(Name = "Route information")]
    public string? RouteInfo { get; set; }

    [Required(ErrorMessage = "Enter the distance."), Range(0.1, 999.99, ErrorMessage = "Distance must be between 0.1 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Choose the event type."), Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    /// <summary>The banner already stored for this event (kept when editing unless a new one is uploaded).</summary>
    public string? ExistingImageUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Name the category, e.g. 10 km or Under 20."), StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = "";

    [Required(ErrorMessage = "Enter the distance."), Range(0.1, 999.99, ErrorMessage = "Distance must be between 0.1 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Enter the entry fee (0 for free)."), Range(0, 999999.99, ErrorMessage = "Enter a fee of 0 or more.")]
    [Display(Name = "Entry fee")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Enter the maximum field size (0 for no limit)."), Range(0, 1000000, ErrorMessage = "Enter 0 or more.")]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; }
}

public class CategoriesPageViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public CategoryFormViewModel Form { get; set; } = new();
}

public class RosterViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }

    [Display(Name = "Finish time")]
    public string? FinishTimeText { get; set; }

    [Range(1, 1000000, ErrorMessage = "Position must be 1 or more.")]
    public int? Position { get; set; }

    [Required] public string Status { get; set; } = "Finished";
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = null!;
    public ResultDto? Result { get; set; }
}

public class ResultsPageViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public List<ResultRow> Rows { get; set; } = new();
}

/// <summary>Used by the organiser pages that first ask "which event?".</summary>
public class EventPickerViewModel
{
    public string Title { get; set; } = "";
    public string Intro { get; set; } = "";
    public string ActionLabel { get; set; } = "Open";
    public string Icon { get; set; } = "bi-calendar-event";
    /// <summary>Builds the link for one event.</summary>
    public Func<EventDto, string> LinkFor { get; set; } = _ => "#";
    public List<EventDto> Events { get; set; } = new();
}

public class StatusPageViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
}

// ---------------- small partial-view models ----------------

public record AvatarModel(string? Url, string Initials, bool Large = false, string Alt = "");
public record BannerModel(string? Url, string EventType, string Alt, bool Hero = false, bool ShowTypeChip = true);
public record PageHead(string Title, string? Subtitle = null, string? BackLabel = null, string? BackUrl = null);
public record EventCardModel(EventDto Event, bool Enrolled);

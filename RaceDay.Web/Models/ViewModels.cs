using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class ErrorViewModel
{
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Please try again.";
    public bool ApiUnavailable { get; set; }
    public int? StatusCode { get; set; }
}

// ---------------- account ----------------
public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100, ErrorMessage = "Name is too long.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type your password again.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose how you'll use RaceDay.")]
    public string Role { get; set; } = "Participant";
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

// ---------------- organiser ----------------
public class DashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public List<EventDto> Upcoming => Events.Where(e => e.EventDate >= DateTime.Today).OrderBy(e => e.EventDate).ToList();
    public int TotalEvents => Events.Count;
    public int UpcomingCount => Upcoming.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public EventDto? NextEvent => Upcoming.FirstOrDefault();
    public int MaxEnrolments => Events.Count == 0 ? 0 : Events.Max(e => e.EnrolmentCount);
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Give the event a name.")]
    [StringLength(150, ErrorMessage = "Name is too long (150 characters max).")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the date and start time.")]
    [Display(Name = "Date & start time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Enter the location.")]
    [StringLength(200, ErrorMessage = "Location is too long.")]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [StringLength(2000, ErrorMessage = "Description is too long (2000 characters max).")]
    public string? Description { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }

    public bool IsEdit => EventId > 0;
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Give the category a name.")]
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose Age or Distance.")]
    [Display(Name = "Category type")]
    public string CategoryType { get; set; } = "Distance";

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Range(0, 120, ErrorMessage = "Minimum age must be 0–120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be 0–120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(1, 100000, ErrorMessage = "Capacity must be at least 1.")]
    [Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000, ErrorMessage = "Entry fee can't be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }
}

public class ManageCategoriesViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
    public bool Editing => Form.CategoryId > 0;
}

public class ManageEnrolmentsViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? Selected { get; set; }
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public string? StatusFilter { get; set; }
}

public class ResultFormViewModel
{
    public int EnrolmentId { get; set; }
    public int ResultId { get; set; }

    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use the format hh:mm:ss, e.g. 01:45:30.")]
    [Display(Name = "Finish time")]
    public string? FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";

    public int EventId { get; set; }
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ManageResultsViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventDto? Selected { get; set; }
    public List<ResultRow> Rows { get; set; } = new();
}

// ---------------- participant ----------------
public class EventsIndexViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Distance { get; set; }    // "short" | "medium" | "long"
    public bool ShowPast { get; set; }
    public int TotalUpcoming { get; set; }
    public bool HasFilter => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Type)
                             || !string.IsNullOrWhiteSpace(Distance) || ShowPast;
}

public class EventDetailViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public WeatherInfo? Weather { get; set; }
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
    public bool IsPast => Event.EventDate < DateTime.Now;
}

public class MyEventsViewModel
{
    public List<EnrolmentDto> Upcoming { get; set; } = new();
    public List<EnrolmentDto> Past { get; set; } = new();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

// These entities mirror the Part 1 ERD and raceday-database.sql
// (table names, column names, types, keys and constraints).

public class Role
{
    public int RoleId { get; set; }
    [Required, MaxLength(20), Column(TypeName = "varchar(20)")]
    public string RoleName { get; set; } = "";
}

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    [Required, MaxLength(100), Column(TypeName = "varchar(100)")]
    public string FullName { get; set; } = "";
    [Required, MaxLength(150), Column(TypeName = "varchar(150)")]
    public string Email { get; set; } = "";
    [Required, MaxLength(255), Column(TypeName = "varchar(255)")]
    public string PasswordHash { get; set; } = "";
    [MaxLength(20), Column(TypeName = "varchar(20)")]
    public string? PhoneNumber { get; set; }
    // Added in Part 3: points at the profile picture stored in Azure Blob Storage (served by the API).
    [MaxLength(500), Column(TypeName = "varchar(500)")]
    public string? ProfilePictureUrl { get; set; }
    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }
    [Required, MaxLength(150), Column(TypeName = "varchar(150)")]
    public string EventName { get; set; } = "";
    [MaxLength(1000), Column(TypeName = "varchar(1000)")]
    public string? Description { get; set; }
    [Column(TypeName = "datetime")]
    public DateTime EventDate { get; set; }
    [Required, MaxLength(150), Column(TypeName = "varchar(150)")]
    public string Location { get; set; } = "";
    [MaxLength(500), Column(TypeName = "varchar(500)")]
    public string? RouteInfo { get; set; }
    [MaxLength(500), Column(TypeName = "varchar(500)")]
    public string? ImageUrl { get; set; }

    // Added because the Part 2 brief says each event must capture distance and event type.
    // (They are not in the Part 1 ERD - see docs/part2-notes.md.)
    [Column(TypeName = "decimal(5,2)")]
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; } = EventType.Run;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Category> Categories { get; set; } = new();
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    [Required, MaxLength(100), Column(TypeName = "varchar(100)")]
    public string CategoryName { get; set; } = "";
    [Column(TypeName = "decimal(5,2)")]
    public decimal DistanceKm { get; set; }
    [Column(TypeName = "decimal(8,2)")]
    public decimal Price { get; set; }
    public int MaxParticipants { get; set; }
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public User? Participant { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    [Column(TypeName = "datetime")]
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    [Required, MaxLength(20), Column(TypeName = "varchar(20)")]
    public string Status { get; set; } = "Confirmed";
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    [Required, MaxLength(20), Column(TypeName = "varchar(20)")]
    public string Status { get; set; } = "Finished";
    [Column(TypeName = "datetime")]
    public DateTime CapturedAt { get; set; } = DateTime.Now;
}

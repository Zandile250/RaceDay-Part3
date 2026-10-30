namespace RaceDay.Api.Models;

/// <summary>The two system roles. Their names match the rows in the Roles table.</summary>
public enum UserRole { Organiser, Participant }

/// <summary>Event type required by the Part 2 brief (run, walk or cycle).</summary>
public enum EventType { Run, Walk, Cycle }

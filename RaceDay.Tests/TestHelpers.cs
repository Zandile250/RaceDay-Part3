using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Tests;

/// <summary>Minimal in-memory session so controllers can be tested without a web server.</summary>
public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();
    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;
    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
}

public static class TestHelpers
{
    /// <summary>Fresh in-memory database. EnsureCreated also applies the seeded Roles rows.</summary>
    public static AppDbContext NewDb()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Database.EnsureCreated();
        return db;
    }

    public static IConfiguration NewConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Organiser:InviteCode"] = "TEST-CODE"
        }).Build();

    public static HttpContext NewHttpContext(int? userId = null, string? role = null)
    {
        var ctx = new DefaultHttpContext { Session = new TestSession() };
        if (userId is not null) ctx.Session.SetInt32(SessionKeys.UserId, userId.Value);
        if (role is not null) ctx.Session.SetString(SessionKeys.Role, role);
        return ctx;
    }

    public static T WithSession<T>(this T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = NewHttpContext(userId, role) };
        return controller;
    }

    public static User AddUser(this AppDbContext db, string email, string role)
    {
        var roleId = db.Roles.Single(r => r.RoleName == role).RoleId;
        var u = new User
        {
            RoleId = roleId,
            FullName = "Test " + role,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
        };
        db.Users.Add(u);
        db.SaveChanges();
        return u;
    }

    public static Event AddEvent(this AppDbContext db, int organiserId, string name = "Soweto 10km")
    {
        var ev = new Event
        {
            OrganiserId = organiserId, EventName = name, Description = "Fun run",
            EventDate = DateTime.Now.AddMonths(2), Location = "Soweto",
            DistanceKm = 10, EventType = EventType.Run
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(this AppDbContext db, int eventId, string name = "10km", int max = 0)
    {
        var c = new Category { EventId = eventId, CategoryName = name, DistanceKm = 10, Price = 100, MaxParticipants = max };
        db.Categories.Add(c);
        db.SaveChanges();
        return c;
    }

    public static Enrolment AddEnrolment(this AppDbContext db, int participantId, int categoryId)
    {
        var e = new Enrolment { ParticipantId = participantId, CategoryId = categoryId };
        db.Enrolments.Add(e);
        db.SaveChanges();
        return e;
    }
}

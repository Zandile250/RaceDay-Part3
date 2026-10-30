using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class EventsControllerTests
{
    private static EventRequest NewEvent() => new()
    {
        EventName = "Soweto 10km", Description = "Fun run", EventDate = DateTime.Now.AddMonths(2),
        Location = "Soweto", RouteInfo = "Out and back", DistanceKm = 10, EventType = EventType.Run
    };

    [Fact]
    public async Task Create_AsOrganiser_SavesEventAgainstTheOrganiser()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var controller = new EventsController(db).WithSession(org.UserId, "Organiser");

        var result = await controller.Create(NewEvent());

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(org.UserId, db.Events.Single().OrganiserId);
    }

    [Fact]
    public async Task Update_OtherOrganisersEvent_IsForbidden()
    {
        using var db = TestHelpers.NewDb();
        var owner = db.AddUser("owner@x.com", "Organiser");
        var other = db.AddUser("other@x.com", "Organiser");
        var ev = db.AddEvent(owner.UserId, "Original");
        var controller = new EventsController(db).WithSession(other.UserId, "Organiser");

        var result = await controller.Update(ev.EventId, NewEvent());

        Assert.Equal(403, ((ObjectResult)result).StatusCode);
        Assert.Equal("Original", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Update_OwnEvent_ChangesIt()
    {
        using var db = TestHelpers.NewDb();
        var owner = db.AddUser("owner@x.com", "Organiser");
        var ev = db.AddEvent(owner.UserId, "Original");
        var controller = new EventsController(db).WithSession(owner.UserId, "Organiser");

        var result = await controller.Update(ev.EventId, NewEvent());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Soweto 10km", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Delete_OwnEventWithoutEnrolments_RemovesEventAndItsCategories()
    {
        using var db = TestHelpers.NewDb();
        var owner = db.AddUser("owner@x.com", "Organiser");
        var ev = db.AddEvent(owner.UserId);
        db.AddCategory(ev.EventId);
        var controller = new EventsController(db).WithSession(owner.UserId, "Organiser");

        var result = await controller.Delete(ev.EventId);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Events);
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_ReturnsConflict()
    {
        using var db = TestHelpers.NewDb();
        var owner = db.AddUser("owner@x.com", "Organiser");
        var part = db.AddUser("part@x.com", "Participant");
        var ev = db.AddEvent(owner.UserId);
        var cat = db.AddCategory(ev.EventId);
        db.AddEnrolment(part.UserId, cat.CategoryId);
        var controller = new EventsController(db).WithSession(owner.UserId, "Organiser");

        var result = await controller.Delete(ev.EventId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task GetAll_IsPublic_AndReturnsEvents()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        db.AddEvent(org.UserId);
        var controller = new EventsController(db).WithSession();   // no one logged in

        var ok = Assert.IsType<OkObjectResult>(await controller.GetAll());

        Assert.Single(Assert.IsType<List<EventResponse>>(ok.Value));
    }

    [Fact]
    public async Task GetById_IncludesCategories()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        db.AddCategory(ev.EventId, "10km");
        var controller = new EventsController(db).WithSession();

        var ok = Assert.IsType<OkObjectResult>(await controller.GetById(ev.EventId));

        var detail = Assert.IsType<EventDetailResponse>(ok.Value);
        Assert.Single(detail.Categories);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        using var db = TestHelpers.NewDb();
        var controller = new EventsController(db).WithSession();

        Assert.IsType<NotFoundResult>(await controller.GetById(999));
    }
}

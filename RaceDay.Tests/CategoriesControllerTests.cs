using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class CategoriesControllerTests
{
    private static CategoryRequest NewCategory() =>
        new() { CategoryName = "21km", DistanceKm = 21.1m, Price = 250, MaxParticipants = 100 };

    [Fact]
    public async Task Create_OnOwnEvent_SavesCategory()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        var controller = new CategoriesController(db).WithSession(org.UserId, "Organiser");

        var result = await controller.Create(ev.EventId, NewCategory());

        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        Assert.Equal("21km", db.Categories.Single().CategoryName);
    }

    [Fact]
    public async Task Create_OnAnotherOrganisersEvent_IsForbidden()
    {
        using var db = TestHelpers.NewDb();
        var owner = db.AddUser("owner@x.com", "Organiser");
        var other = db.AddUser("other@x.com", "Organiser");
        var ev = db.AddEvent(owner.UserId);
        var controller = new CategoriesController(db).WithSession(other.UserId, "Organiser");

        var result = await controller.Create(ev.EventId, NewCategory());

        Assert.Equal(403, ((ObjectResult)result).StatusCode);
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Update_OwnCategory_ChangesPrice()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var cat = db.AddCategory(db.AddEvent(org.UserId).EventId);
        var controller = new CategoriesController(db).WithSession(org.UserId, "Organiser");

        var result = await controller.Update(cat.CategoryId, NewCategory());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(250, db.Categories.Single().Price);
    }

    [Fact]
    public async Task Delete_CategoryWithEnrolments_ReturnsConflict()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var part = db.AddUser("part@x.com", "Participant");
        var cat = db.AddCategory(db.AddEvent(org.UserId).EventId);
        db.AddEnrolment(part.UserId, cat.CategoryId);
        var controller = new CategoriesController(db).WithSession(org.UserId, "Organiser");

        var result = await controller.Delete(cat.CategoryId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task GetForEvent_IsPublic()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        db.AddCategory(ev.EventId);
        var controller = new CategoriesController(db).WithSession();

        var ok = Assert.IsType<OkObjectResult>(await controller.GetForEvent(ev.EventId));

        Assert.Single(Assert.IsType<List<CategoryResponse>>(ok.Value));
    }
}

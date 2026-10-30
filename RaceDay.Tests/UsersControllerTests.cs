using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task GetMe_ReturnsOwnProfileWithRole()
    {
        using var db = TestHelpers.NewDb();
        var user = db.AddUser("me@x.com", "Organiser");
        var controller = new UsersController(db).WithSession(user.UserId, "Organiser");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetMe());

        var profile = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal("me@x.com", profile.Email);
        Assert.Equal("Organiser", profile.Role);
    }

    [Fact]
    public async Task UpdateMe_ChangesNameAndPhone()
    {
        using var db = TestHelpers.NewDb();
        var user = db.AddUser("me@x.com", "Participant");
        var controller = new UsersController(db).WithSession(user.UserId, "Participant");

        var result = await controller.UpdateMe(new UpdateProfileRequest { FullName = "New Name", PhoneNumber = "0821234567" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("New Name", db.Users.Single().FullName);
        Assert.Equal("0821234567", db.Users.Single().PhoneNumber);
    }
}

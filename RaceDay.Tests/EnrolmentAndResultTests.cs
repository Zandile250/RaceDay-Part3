using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class EnrolmentAndResultTests
{
    private static (RaceDay.Api.Data.AppDbContext db, User org, User part, Event ev, Category cat) Seed(int max = 0)
    {
        var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var part = db.AddUser("part@x.com", "Participant");
        var ev = db.AddEvent(org.UserId, "Cape Cycle");
        var cat = db.AddCategory(ev.EventId, "Senior", max);
        return (db, org, part, ev, cat);
    }

    // ---- Enrolments ----

    [Fact]
    public async Task Enrol_ValidCategory_RecordsParticipantAndCategory()
    {
        var (db, _, part, ev, cat) = Seed();
        var controller = new EnrolmentsController(db).WithSession(part.UserId, "Participant");

        var result = await controller.Enrol(new EnrolRequest { CategoryId = cat.CategoryId });

        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        var e = db.Enrolments.Single();
        Assert.Equal((part.UserId, cat.CategoryId, "Confirmed"), (e.ParticipantId, e.CategoryId, e.Status));
    }

    [Fact]
    public async Task Enrol_SameCategoryTwice_ReturnsConflict()
    {
        var (db, _, part, _, cat) = Seed();
        var controller = new EnrolmentsController(db).WithSession(part.UserId, "Participant");
        await controller.Enrol(new EnrolRequest { CategoryId = cat.CategoryId });

        var result = await controller.Enrol(new EnrolRequest { CategoryId = cat.CategoryId });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_ReturnsNotFound()
    {
        var (db, _, part, _, _) = Seed();
        var controller = new EnrolmentsController(db).WithSession(part.UserId, "Participant");

        var result = await controller.Enrol(new EnrolRequest { CategoryId = 12345 });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Enrol_FullCategory_ReturnsConflict()
    {
        var (db, _, part, _, cat) = Seed(max: 1);
        var other = db.AddUser("p2@x.com", "Participant");
        db.AddEnrolment(other.UserId, cat.CategoryId);
        var controller = new EnrolmentsController(db).WithSession(part.UserId, "Participant");

        var result = await controller.Enrol(new EnrolRequest { CategoryId = cat.CategoryId });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Mine_ReturnsOnlyOwnEnrolments()
    {
        var (db, _, part, _, cat) = Seed();
        var other = db.AddUser("p2@x.com", "Participant");
        db.AddEnrolment(part.UserId, cat.CategoryId);
        db.AddEnrolment(other.UserId, cat.CategoryId);
        var controller = new EnrolmentsController(db).WithSession(part.UserId, "Participant");

        var ok = Assert.IsType<OkObjectResult>(await controller.Mine());

        Assert.Single(Assert.IsType<List<EnrolmentResponse>>(ok.Value));
    }

    [Fact]
    public async Task ForEvent_OtherOrganiser_IsForbidden()
    {
        var (db, _, _, ev, _) = Seed();
        var other = db.AddUser("other@x.com", "Organiser");
        var controller = new EnrolmentsController(db).WithSession(other.UserId, "Organiser");

        var result = await controller.ForEvent(ev.EventId);

        Assert.Equal(403, ((ObjectResult)result).StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnEnrolment_RemovesIt()
    {
        var (db, _, part, _, cat) = Seed();
        var enrolment = db.AddEnrolment(part.UserId, cat.CategoryId);
        var controller = new EnrolmentsController(db).WithSession(part.UserId, "Participant");

        var result = await controller.Cancel(enrolment.EnrolmentId);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_IsForbidden()
    {
        var (db, _, part, _, cat) = Seed();
        var other = db.AddUser("p2@x.com", "Participant");
        var enrolment = db.AddEnrolment(part.UserId, cat.CategoryId);
        var controller = new EnrolmentsController(db).WithSession(other.UserId, "Participant");

        var result = await controller.Cancel(enrolment.EnrolmentId);

        Assert.Equal(403, ((ObjectResult)result).StatusCode);
        Assert.Single(db.Enrolments);
    }

    // ---- Results ----

    [Fact]
    public async Task CaptureResult_ForOwnEvent_SavesResult()
    {
        var (db, org, part, _, cat) = Seed();
        var enrolment = db.AddEnrolment(part.UserId, cat.CategoryId);
        var controller = new ResultsController(db).WithSession(org.UserId, "Organiser");

        var result = await controller.Capture(new CaptureResultRequest
        { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(95), Position = 3, Status = "Finished" });

        Assert.Equal(201, ((ObjectResult)result).StatusCode);
        Assert.Equal(3, db.Results.Single().Position);
    }

    [Fact]
    public async Task CaptureResult_ForAnotherOrganisersEvent_IsForbidden()
    {
        var (db, _, part, _, cat) = Seed();
        var otherOrg = db.AddUser("other@x.com", "Organiser");
        var enrolment = db.AddEnrolment(part.UserId, cat.CategoryId);
        var controller = new ResultsController(db).WithSession(otherOrg.UserId, "Organiser");

        var result = await controller.Capture(new CaptureResultRequest
        { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(95), Position = 1, Status = "Finished" });

        Assert.Equal(403, ((ObjectResult)result).StatusCode);
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var (db, org, part, _, cat) = Seed();
        var enrolment = db.AddEnrolment(part.UserId, cat.CategoryId);
        var controller = new ResultsController(db).WithSession(org.UserId, "Organiser");
        var req = new CaptureResultRequest { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(60), Position = 1, Status = "Finished" };
        await controller.Capture(req);

        var result = await controller.Capture(req);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdateResult_OwnEvent_CorrectsPosition()
    {
        var (db, org, part, _, cat) = Seed();
        var enrolment = db.AddEnrolment(part.UserId, cat.CategoryId);
        db.Results.Add(new Result { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(70), Position = 5 });
        db.SaveChanges();
        var controller = new ResultsController(db).WithSession(org.UserId, "Organiser");

        var result = await controller.Update(db.Results.Single().ResultId,
            new UpdateResultRequest { FinishTime = TimeSpan.FromMinutes(69), Position = 4, Status = "Finished" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(4, db.Results.Single().Position);
    }

    [Fact]
    public async Task MyResults_ReturnsOnlyTheParticipantsOwnResults()
    {
        var (db, _, part, _, cat) = Seed();
        var other = db.AddUser("p2@x.com", "Participant");
        var mine = db.AddEnrolment(part.UserId, cat.CategoryId);
        var theirs = db.AddEnrolment(other.UserId, cat.CategoryId);
        db.Results.AddRange(
            new Result { EnrolmentId = mine.EnrolmentId, FinishTime = TimeSpan.FromMinutes(60), Position = 1 },
            new Result { EnrolmentId = theirs.EnrolmentId, FinishTime = TimeSpan.FromMinutes(70), Position = 2 });
        db.SaveChanges();
        var controller = new ResultsController(db).WithSession(part.UserId, "Participant");

        var ok = Assert.IsType<OkObjectResult>(await controller.Mine());

        var list = Assert.IsType<List<ResultResponse>>(ok.Value);
        Assert.Single(list);
        Assert.Equal(1, list[0].Position);
    }

    [Fact]
    public async Task EventResults_ArePublicAndOrderedByPosition()
    {
        var (db, _, part, ev, cat) = Seed();
        var other = db.AddUser("p2@x.com", "Participant");
        var a = db.AddEnrolment(part.UserId, cat.CategoryId);
        var b = db.AddEnrolment(other.UserId, cat.CategoryId);
        db.Results.AddRange(
            new Result { EnrolmentId = a.EnrolmentId, Position = 2 },
            new Result { EnrolmentId = b.EnrolmentId, Position = 1 });
        db.SaveChanges();
        var controller = new ResultsController(db).WithSession();   // no one logged in

        var ok = Assert.IsType<OkObjectResult>(await controller.ForEvent(ev.EventId));

        var list = Assert.IsType<List<ResultResponse>>(ok.Value);
        Assert.Equal(new int?[] { 1, 2 }, list.Select(r => r.Position).ToArray());
    }
}

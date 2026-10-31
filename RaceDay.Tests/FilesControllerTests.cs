using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>In-memory stand-in for Azure Blob Storage, so the upload rules can be tested without Azure.</summary>
public class FakeFileStorage : IFileStorage
{
    public Dictionary<string, (byte[] Bytes, string ContentType)> Files { get; } = new();

    public Task<string> SaveAsync(string folder, Stream content, string contentType, string extension, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        var name = $"{folder}/{Guid.NewGuid():N}{extension}";
        Files[name] = (ms.ToArray(), contentType);
        return Task.FromResult(name);
    }

    public Task<StoredFile?> OpenReadAsync(string blobName, CancellationToken ct = default) =>
        Task.FromResult(Files.TryGetValue(blobName, out var f)
            ? new StoredFile(new MemoryStream(f.Bytes), f.ContentType)
            : null);

    public Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        Files.Remove(blobName);
        return Task.CompletedTask;
    }
}

public class FilesControllerTests
{
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };

    private static IFormFile Upload(byte[] bytes, string contentType = "image/png", string fileName = "banner.png")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task UploadBanner_ByOwner_StoresImageAndSetsImageUrl()
    {
        using var db = TestHelpers.NewDb();
        var storage = new FakeFileStorage();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        var controller = new FilesController(db, storage).WithSession(org.UserId, "Organiser");

        var result = await controller.UploadBanner(ev.EventId, Upload(PngBytes), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ImageUploadResponse>(ok.Value);
        Assert.StartsWith("api/files/events/", body.ImageUrl);
        Assert.Equal(body.ImageUrl, db.Events.Single().ImageUrl);
        Assert.Single(storage.Files);
    }

    [Fact]
    public async Task UploadBanner_ForAnotherOrganisersEvent_IsForbiddenAndStoresNothing()
    {
        using var db = TestHelpers.NewDb();
        var storage = new FakeFileStorage();
        var owner = db.AddUser("owner@x.com", "Organiser");
        var other = db.AddUser("other@x.com", "Organiser");
        var ev = db.AddEvent(owner.UserId);
        var controller = new FilesController(db, storage).WithSession(other.UserId, "Organiser");

        var result = await controller.UploadBanner(ev.EventId, Upload(PngBytes), CancellationToken.None);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task UploadBanner_ForMissingEvent_IsNotFound()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var controller = new FilesController(db, new FakeFileStorage()).WithSession(org.UserId, "Organiser");

        var result = await controller.UploadBanner(999, Upload(PngBytes), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UploadBanner_WithNonImageContentType_IsRejected()
    {
        using var db = TestHelpers.NewDb();
        var storage = new FakeFileStorage();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        var controller = new FilesController(db, storage).WithSession(org.UserId, "Organiser");

        var result = await controller.UploadBanner(ev.EventId, Upload(PngBytes, "text/plain", "notes.txt"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task UploadBanner_WhenBytesAreNotAnImage_IsRejected()
    {
        using var db = TestHelpers.NewDb();
        var storage = new FakeFileStorage();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        var controller = new FilesController(db, storage).WithSession(org.UserId, "Organiser");
        var fake = System.Text.Encoding.ASCII.GetBytes("MZ this is really a program, not a png");

        var result = await controller.UploadBanner(ev.EventId, Upload(fake), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(storage.Files);
    }

    [Fact]
    public async Task UploadBanner_TooLarge_IsRejected()
    {
        using var db = TestHelpers.NewDb();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        var controller = new FilesController(db, new FakeFileStorage()).WithSession(org.UserId, "Organiser");
        var big = new byte[5 * 1024 * 1024 + 1];
        PngBytes.CopyTo(big, 0);

        var result = await controller.UploadBanner(ev.EventId, Upload(big), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadBanner_Replacing_DeletesTheOldImage()
    {
        using var db = TestHelpers.NewDb();
        var storage = new FakeFileStorage();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        var controller = new FilesController(db, storage).WithSession(org.UserId, "Organiser");

        await controller.UploadBanner(ev.EventId, Upload(PngBytes), CancellationToken.None);
        await controller.UploadBanner(ev.EventId, Upload(PngBytes), CancellationToken.None);

        Assert.Single(storage.Files);
    }

    [Fact]
    public async Task UploadProfilePicture_SetsTheLoggedInUsersPicture()
    {
        using var db = TestHelpers.NewDb();
        var storage = new FakeFileStorage();
        var user = db.AddUser("runner@x.com", "Participant");
        var controller = new FilesController(db, storage).WithSession(user.UserId, "Participant");

        var result = await controller.UploadProfilePicture(Upload(PngBytes), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.StartsWith("api/files/profiles/", db.Users.Single().ProfilePictureUrl);
    }

    [Fact]
    public async Task Download_ReturnsTheStoredImage()
    {
        using var db = TestHelpers.NewDb();
        var storage = new FakeFileStorage();
        var org = db.AddUser("org@x.com", "Organiser");
        var ev = db.AddEvent(org.UserId);
        var controller = new FilesController(db, storage).WithSession(org.UserId, "Organiser");
        var upload = Assert.IsType<OkObjectResult>(
            await controller.UploadBanner(ev.EventId, Upload(PngBytes), CancellationToken.None));
        var blobName = ((ImageUploadResponse)upload.Value!).ImageUrl["api/files/".Length..];

        var result = await controller.Download(blobName, CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", file.ContentType);
    }

    [Theory]
    [InlineData("../appsettings.json")]
    [InlineData("events/not-a-real-name.png")]
    [InlineData("secrets/0123456789abcdef0123456789abcdef.png")]
    public async Task Download_WithUnexpectedName_IsNotFound(string name)
    {
        using var db = TestHelpers.NewDb();
        var controller = new FilesController(db, new FakeFileStorage()).WithSession();

        var result = await controller.Download(name, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}

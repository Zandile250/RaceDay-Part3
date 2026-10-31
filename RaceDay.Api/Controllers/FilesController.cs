using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Image upload and download, backed by Azure Blob Storage. Part 3 adds event banners and profile pictures.
/// The MVC application never talks to Azure: it uploads to and downloads from these endpoints.
/// </summary>
[Route("api")]
public class FilesController : ApiControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    // Only names this API generated are served: a folder, 32 hex characters and a known extension.
    private static readonly Regex BlobNamePattern =
        new(@"^(events|profiles)/[0-9a-f]{32}\.(jpg|png|webp|gif)$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;

    public FilesController(AppDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>Upload (or replace) the banner image of an event you own (Organiser only).</summary>
    /// <remarks>Send the image as multipart/form-data in a field called <c>file</c>. JPG, PNG, WebP or GIF, up to 5 MB.</remarks>
    /// <response code="200">Banner stored; returns the new image URL.</response>
    /// <response code="400">No file, wrong type, or too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("events/{eventId:int}/banner")]
    [SessionAuthorize(Roles = "Organiser")]
    [RequestSizeLimit(MaxBytes + 1_048_576)]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int eventId, IFormFile file, CancellationToken ct)
    {
        var ev = await _db.Events.FindAsync(new object[] { eventId }, ct);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbidden("You can only change your own events.");

        var (blobName, error) = await StoreImageAsync(file, "events", ct);
        if (error is not null) return error;

        var oldBlob = FileUrls.ToBlobName(ev.ImageUrl);
        ev.ImageUrl = FileUrls.ToUrl(blobName!);
        await _db.SaveChangesAsync(ct);
        if (oldBlob is not null) await _storage.DeleteAsync(oldBlob, ct);

        return Ok(new ImageUploadResponse(ev.ImageUrl));
    }

    /// <summary>Upload (or replace) your own profile picture (Participant or Organiser).</summary>
    /// <remarks>Send the image as multipart/form-data in a field called <c>file</c>. JPG, PNG, WebP or GIF, up to 5 MB.</remarks>
    /// <response code="200">Picture stored; returns the new image URL.</response>
    /// <response code="400">No file, wrong type, or too large.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("users/me/picture")]
    [SessionAuthorize]
    [RequestSizeLimit(MaxBytes + 1_048_576)]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { CurrentUserId }, ct);
        if (user is null) return NotFound();

        var (blobName, error) = await StoreImageAsync(file, "profiles", ct);
        if (error is not null) return error;

        var oldBlob = FileUrls.ToBlobName(user.ProfilePictureUrl);
        user.ProfilePictureUrl = FileUrls.ToUrl(blobName!);
        await _db.SaveChangesAsync(ct);
        if (oldBlob is not null) await _storage.DeleteAsync(oldBlob, ct);

        return Ok(new ImageUploadResponse(user.ProfilePictureUrl));
    }

    /// <summary>Download a stored image (public, so it can be shown on pages).</summary>
    /// <response code="200">The image.</response>
    /// <response code="404">No such image.</response>
    [HttpGet("files/{**blobName}")]
    [Produces("image/jpeg", "image/png", "image/webp", "image/gif")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string blobName, CancellationToken ct)
    {
        if (!BlobNamePattern.IsMatch(blobName)) return NotFound();

        var stored = await _storage.OpenReadAsync(blobName, ct);
        if (stored is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=3600";
        return File(stored.Content, stored.ContentType);
    }

    // ---- helpers ----

    private async Task<(string? BlobName, IActionResult? Error)> StoreImageAsync(
        IFormFile? file, string folder, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return (null, BadRequest(new { message = "Please choose an image file to upload." }));
        if (file.Length > MaxBytes)
            return (null, BadRequest(new { message = "The image must be 5 MB or smaller." }));
        if (!AllowedTypes.TryGetValue(file.ContentType ?? "", out var extension))
            return (null, BadRequest(new { message = "Only JPG, PNG, WebP or GIF images are allowed." }));

        await using var stream = file.OpenReadStream();
        if (!await HasImageSignatureAsync(stream, extension, ct))
            return (null, BadRequest(new { message = "That file does not look like a valid image." }));

        stream.Position = 0;
        var blobName = await _storage.SaveAsync(folder, stream, file.ContentType!, extension, ct);
        return (blobName, null);
    }

    /// <summary>Checks the first bytes of the file, so a renamed .exe cannot pass as an image.</summary>
    private static async Task<bool> HasImageSignatureAsync(Stream stream, string extension, CancellationToken ct)
    {
        var h = new byte[12];
        var read = await stream.ReadAtLeastAsync(h, h.Length, throwOnEndOfStream: false, ct);

        return extension switch
        {
            ".jpg" => read >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF,
            ".png" => read >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47,
            ".gif" => read >= 6 && Encoding.ASCII.GetString(h, 0, 4) == "GIF8",
            ".webp" => read >= 12 && Encoding.ASCII.GetString(h, 0, 4) == "RIFF" &&
                       Encoding.ASCII.GetString(h, 8, 4) == "WEBP",
            _ => false
        };
    }
}

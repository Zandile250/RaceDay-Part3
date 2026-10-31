using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Serves event banners and profile pictures. The images live in Azure Blob Storage, which only the API talks to:
/// this action asks the API for the image and passes it on, so the browser only ever talks to this site.
/// </summary>
public class MediaController : Controller
{
    // Same shape the API generates: folder / 32 hex characters . extension
    private static readonly Regex BlobName = new(@"^(events|profiles)/[0-9a-f]{32}\.(jpg|png|webp|gif)$", RegexOptions.Compiled);

    private readonly IRaceDayApi _api;
    public MediaController(IRaceDayApi api) => _api = api;

    [HttpGet("media/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        if (!BlobName.IsMatch(path)) return NotFound();

        var file = await _api.GetFileAsync(path);
        if (file is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=3600";
        return File(file.Bytes, file.ContentType);
    }
}

namespace RaceDay.Api.Services;

/// <summary>A file read back from storage.</summary>
public sealed record StoredFile(Stream Content, string ContentType);

/// <summary>
/// Stores uploaded images. The API is the only part of RaceDay that talks to Azure Blob Storage;
/// the MVC application always goes through the API endpoints.
/// </summary>
public interface IFileStorage
{
    /// <summary>Saves a file under <paramref name="folder"/> and returns its blob name (e.g. "events/ab12.jpg").</summary>
    Task<string> SaveAsync(string folder, Stream content, string contentType, string extension, CancellationToken ct = default);

    /// <summary>Opens a stored file, or returns null when it does not exist.</summary>
    Task<StoredFile?> OpenReadAsync(string blobName, CancellationToken ct = default);

    /// <summary>Deletes a stored file if it exists.</summary>
    Task DeleteAsync(string blobName, CancellationToken ct = default);
}

/// <summary>How stored files are referred to in the database (the ImageUrl / ProfilePictureUrl columns).</summary>
public static class FileUrls
{
    /// <summary>Relative API route that serves a stored file.</summary>
    public const string Prefix = "api/files/";

    public static string ToUrl(string blobName) => Prefix + blobName;

    /// <summary>Returns the blob name behind one of our own URLs, or null (e.g. for an external https:// link).</summary>
    public static string? ToBlobName(string? url) =>
        url is not null && url.StartsWith(Prefix, StringComparison.Ordinal) ? url[Prefix.Length..] : null;
}

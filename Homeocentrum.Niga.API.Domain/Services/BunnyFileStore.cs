using System.Net;
using System.Net.Http.Headers;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Homeocentrum.Niga.API.Domain.Services;

public sealed class BunnyFileStore : IStoredFileStore
{
    public const string HttpClientName = "BunnyStorage";
    public const string Prefix = "bunny:";

    private readonly IHttpClientFactory _http;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BunnyFileStore> _logger;
    private readonly BunnyOptions _options;

    public BunnyFileStore(
        IHttpClientFactory http,
        IWebHostEnvironment env,
        IOptions<BunnyOptions> options,
        ILogger<BunnyFileStore> logger)
    {
        _http = http;
        _env = env;
        _logger = logger;
        _options = options.Value ?? new BunnyOptions();
    }

    public bool RemoteEnabled =>
        _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.AccessKey)
        && !string.IsNullOrWhiteSpace(_options.StorageEndpoint);

    public async Task<string> SaveAsync(string folder, string extension, byte[] content, CancellationToken cancellationToken = default)
    {
        if (content == null || content.Length == 0)
            throw new InvalidOperationException("File is empty.");

        var key = NewKey(folder, extension);
        if (!RemoteEnabled)
            return SaveLocal(key, content);

        var client = _http.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Put, ObjectUrl(key));
        request.Headers.TryAddWithoutValidation("AccessKey", _options.AccessKey.Trim());
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Bunny upload failed with status {Status} for a new object.", (int)response.StatusCode);
            throw new InvalidOperationException("File storage is unavailable.");
        }

        return Prefix + key;
    }

    public async Task<byte[]?> ReadAsync(string contentRoot, string? storedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;
        if (IsRemote(storedPath))
            return await DownloadAsync(ObjectKey(storedPath), cancellationToken);

        var full = LocalFullPath(contentRoot, storedPath);
        if (full == null || !File.Exists(full))
            return null;
        return await File.ReadAllBytesAsync(full, cancellationToken);
    }

    public async Task DeleteAsync(string contentRoot, string? storedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return;
        if (IsRemote(storedPath))
        {
            if (!RemoteEnabled)
                return;
            var client = _http.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Delete, ObjectUrl(ObjectKey(storedPath)));
            request.Headers.TryAddWithoutValidation("AccessKey", _options.AccessKey.Trim());
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                _logger.LogWarning("Bunny delete failed with status {Status}.", (int)response.StatusCode);
            return;
        }

        var full = LocalFullPath(contentRoot, storedPath);
        if (full != null && File.Exists(full))
            File.Delete(full);
    }

    public static bool IsRemote(string? storedPath)
        => !string.IsNullOrWhiteSpace(storedPath)
           && storedPath.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);

    private async Task<byte[]?> DownloadAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (!RemoteEnabled)
            return null;
        var client = _http.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, ObjectUrl(objectKey));
        request.Headers.TryAddWithoutValidation("AccessKey", _options.AccessKey.Trim());
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Bunny download failed with status {Status}.", (int)response.StatusCode);
            throw new InvalidOperationException("File storage is unavailable.");
        }
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private string ObjectUrl(string objectKey)
    {
        var endpoint = _options.StorageEndpoint.Trim().TrimEnd('/');
        return endpoint + "/" + objectKey.TrimStart('/');
    }

    private string SaveLocal(string objectKey, byte[] content)
    {
        var name = Path.GetFileName(objectKey.Replace('/', Path.DirectorySeparatorChar));
        var folder = UploadedMedia.Folder(_env.ContentRootPath, UploadedMedia.SecureDocuments);
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, name), content);
        return UploadedMedia.ContentRelative(UploadedMedia.SecureDocuments, name);
    }

    private static string NewKey(string folder, string extension)
    {
        var cleanFolder = (folder ?? "").Replace('\\', '/').Trim('/');
        if (cleanFolder.Length == 0 || cleanFolder.Contains("..", StringComparison.Ordinal) || cleanFolder.Contains(':'))
            throw new InvalidOperationException("Storage folder is not valid.");
        var ext = (extension ?? "").Trim().ToLowerInvariant();
        if (!ext.StartsWith('.'))
            ext = "." + ext;
        if (ext.Length is < 2 or > 10 || ext.Any(c => !char.IsLetterOrDigit(c) && c != '.'))
            ext = ".bin";
        return "tenants/homeocentrum/" + cleanFolder + "/" + Guid.NewGuid().ToString("N") + ext;
    }

    private static string ObjectKey(string storedPath)
    {
        var key = storedPath[Prefix.Length..].Trim().TrimStart('/');
        if (key.Length == 0 || key.Contains("..", StringComparison.Ordinal) || !key.StartsWith("tenants/homeocentrum/", StringComparison.Ordinal))
            throw new InvalidOperationException("Stored file path is not valid.");
        return key;
    }

    private static string? LocalFullPath(string contentRoot, string storedPath)
    {
        var resolved = UploadedMedia.Resolve(contentRoot, storedPath);
        if (File.Exists(resolved) && IsUnderContent(contentRoot, resolved))
            return resolved;
        foreach (var folder in UploadedMedia.Folders)
        {
            var candidate = UploadedMedia.MediaRelativeToFull(contentRoot, folder, storedPath);
            if (candidate != null && File.Exists(candidate))
                return candidate;
        }
        return IsUnderContent(contentRoot, resolved) ? resolved : null;
    }

    private static bool IsUnderContent(string contentRoot, string full)
    {
        var root = Path.GetFullPath(contentRoot) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(full);
        return path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }
}

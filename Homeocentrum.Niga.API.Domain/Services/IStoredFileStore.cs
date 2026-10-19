namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Saves and reads user files. When Bunny is enabled, new files go to the private zone
/// and the stored path is <c>bunny:{objectKey}</c>. Older disk paths still open.
/// </summary>
public interface IStoredFileStore
{
    bool RemoteEnabled { get; }

    Task<string> SaveAsync(string folder, string extension, byte[] content, CancellationToken cancellationToken = default);

    Task<byte[]?> ReadAsync(string contentRoot, string? storedPath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string contentRoot, string? storedPath, CancellationToken cancellationToken = default);
}

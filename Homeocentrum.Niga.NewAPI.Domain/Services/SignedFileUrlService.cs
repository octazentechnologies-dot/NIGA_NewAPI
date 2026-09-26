using System.Security.Cryptography;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.Services
{
    /// <summary>SEC-05.02 — HMAC signed download URLs for /attachments and /Blogs.</summary>
    public interface ISignedFileUrlService
    {
        string Sign(string root, string relativePath, DateTime expiresAtUtc);
        bool TryValidate(string root, string relativePath, long expUnix, string signature, out string error);
        bool TryResolvePhysicalPath(string contentRoot, string root, string relativePath, out string fullPath, out string error);
    }

    public class SignedFileUrlService : ISignedFileUrlService
    {
        private readonly byte[] _key;

        public SignedFileUrlService(IConfiguration configuration)
        {
            var key = configuration["TokenKey"] ?? "NigaHomeoSignedFileFallbackKey";
            _key = Encoding.UTF8.GetBytes(key);
        }

        public string Sign(string root, string relativePath, DateTime expiresAtUtc)
        {
            var exp = new DateTimeOffset(expiresAtUtc).ToUnixTimeSeconds();
            var payload = Payload(root, relativePath, exp);
            return Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload)));
        }

        public bool TryValidate(string root, string relativePath, long expUnix, string signature, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(signature))
            {
                error = "Signature is required.";
                return false;
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
            {
                error = "Signed URL expired.";
                return false;
            }

            var expected = Sign(root, relativePath, DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime);
            if (!string.Equals(expected, signature.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                error = "Invalid signature.";
                return false;
            }

            return true;
        }

        public bool TryResolvePhysicalPath(
            string contentRoot,
            string root,
            string relativePath,
            out string fullPath,
            out string error)
        {
            fullPath = "";
            error = "";
            if (!IsAllowedRoot(root))
            {
                error = "Root must be attachments or Blogs.";
                return false;
            }

            var safeRelative = (relativePath ?? "").Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrWhiteSpace(safeRelative)
                || safeRelative.Contains("..", StringComparison.Ordinal)
                || Path.IsPathRooted(safeRelative))
            {
                error = "Invalid path.";
                return false;
            }

            var folder = Path.Combine(contentRoot, "Data", root);
            fullPath = Path.GetFullPath(Path.Combine(folder, safeRelative.Replace('/', Path.DirectorySeparatorChar)));
            var folderFull = Path.GetFullPath(folder);
            if (!fullPath.StartsWith(folderFull, StringComparison.OrdinalIgnoreCase))
            {
                error = "Invalid path.";
                return false;
            }

            return true;
        }

        public static bool IsAllowedRoot(string? root)
            => string.Equals(root, "attachments", StringComparison.OrdinalIgnoreCase)
               || string.Equals(root, "Blogs", StringComparison.OrdinalIgnoreCase);

        private static string Payload(string root, string relativePath, long exp)
            => $"{root.Trim()}|{relativePath.Replace('\\', '/').TrimStart('/')}|{exp}";
    }
}

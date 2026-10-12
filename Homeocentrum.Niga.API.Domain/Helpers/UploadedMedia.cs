using Microsoft.Extensions.Logging;

namespace Homeocentrum.Niga.API.Domain.Helpers
{
    /// <summary>
    /// Every uploaded or generated user file lives under ContentRoot/Data/UploadedMedia/{folder}.
    /// Database paths are stored either relative to that root ("DoctorPhotos/x.jpg") or relative to
    /// the content root ("Data/UploadedMedia/SecureDocuments/x.pdf"). Rows written before this folder
    /// existed point at Data/{folder}; <see cref="Resolve"/> maps them to the new location.
    /// </summary>
    public static class UploadedMedia
    {
        public const string RootFolder = "UploadedMedia";

        public const string SecureDocuments = "SecureDocuments";
        public const string DoctorPhotos = "DoctorPhotos";
        public const string DoctorCredentials = "DoctorCredentials";
        public const string Blogs = "Blogs";
        public const string News = "News";
        public const string AudioCaseTaking = "AudioCaseTaking";
        public const string Attachments = "attachments";

        public static readonly string[] Folders =
        {
            SecureDocuments, DoctorPhotos, DoctorCredentials, Blogs, News, AudioCaseTaking, Attachments
        };

        /// <summary>ContentRoot/Data/UploadedMedia</summary>
        public static string Root(string contentRoot)
            => Path.Combine(contentRoot, "Data", RootFolder);

        /// <summary>ContentRoot/Data/UploadedMedia/{folder}</summary>
        public static string Folder(string contentRoot, string folder)
            => Path.Combine(Root(contentRoot), folder);

        /// <summary>"Data/UploadedMedia/{folder}/{fileName}" — for columns stored relative to the content root.</summary>
        public static string ContentRelative(string folder, string fileName)
            => $"Data/{RootFolder}/{folder}/{fileName}";

        /// <summary>"{folder}/{fileName}" — for columns stored relative to the media root.</summary>
        public static string MediaRelative(string folder, string fileName)
            => $"{folder}/{fileName}";

        /// <summary>
        /// Full path for a path relative to the media root ("DoctorPhotos/x.jpg"), or null when it escapes the folder.
        /// </summary>
        public static string? MediaRelativeToFull(string contentRoot, string folder, string? relative)
        {
            if (string.IsNullOrWhiteSpace(relative))
                return null;
            var clean = relative.Replace('\\', '/').TrimStart('/');
            if (!clean.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                return null;
            var folderFull = Path.GetFullPath(Folder(contentRoot, folder));
            var full = Path.GetFullPath(Path.Combine(Root(contentRoot), clean.Replace('/', Path.DirectorySeparatorChar)));
            return full.StartsWith(folderFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? full : null;
        }

        /// <summary>
        /// Resolve a stored path (absolute, or relative to the content root) to the file on disk.
        /// Paths under Data/{folder} from before UploadedMedia are mapped to Data/UploadedMedia/{folder}.
        /// Returns the best candidate even when the file is missing.
        /// </summary>
        public static string Resolve(string contentRoot, string storedPath)
        {
            var full = Path.IsPathRooted(storedPath)
                ? storedPath
                : Path.GetFullPath(Path.Combine(contentRoot, storedPath.Replace('/', Path.DirectorySeparatorChar)));
            if (File.Exists(full))
                return full;

            var sep = Path.DirectorySeparatorChar;
            var dataMarker = $"{sep}Data{sep}";
            var at = full.LastIndexOf(dataMarker, StringComparison.OrdinalIgnoreCase);
            if (at < 0)
                return full;

            var afterData = full[(at + dataMarker.Length)..];
            if (afterData.StartsWith(RootFolder + sep, StringComparison.OrdinalIgnoreCase))
                return full;

            var candidate = Path.Combine(Root(contentRoot), afterData);
            return File.Exists(candidate) ? candidate : full;
        }

        /// <summary>
        /// Create Data/UploadedMedia/{folder} for every folder and move files left in the old Data/{folder}.
        /// Safe to run on every start: files already present at the destination are left in place.
        /// </summary>
        public static void EnsureFolders(string contentRoot, ILogger? logger = null)
        {
            foreach (var folder in Folders)
            {
                var target = Folder(contentRoot, folder);
                Directory.CreateDirectory(target);

                var legacy = Path.Combine(contentRoot, "Data", folder);
                if (!Directory.Exists(legacy))
                    continue;

                var moved = 0;
                foreach (var file in Directory.EnumerateFiles(legacy, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(legacy, file);
                    var destination = Path.Combine(target, relative);
                    if (File.Exists(destination))
                        continue;
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                        File.Move(file, destination);
                        moved++;
                    }
                    catch (IOException ex)
                    {
                        logger?.LogWarning(ex, "Could not move {File} to {Destination}", file, destination);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        logger?.LogWarning(ex, "Could not move {File} to {Destination}", file, destination);
                    }
                }

                if (moved > 0)
                    logger?.LogInformation("Moved {Count} file(s) from Data/{Folder} to Data/{Root}/{Folder}", moved, folder, RootFolder, folder);

                TryRemoveEmpty(legacy);
            }
        }

        private static void TryRemoveEmpty(string directory)
        {
            try
            {
                foreach (var child in Directory.EnumerateDirectories(directory))
                    TryRemoveEmpty(child);
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

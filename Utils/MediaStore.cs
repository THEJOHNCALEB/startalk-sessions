using Microsoft.AspNetCore.Hosting;

namespace StarTalkSessions.Utils
{
    /// <summary>
    /// Local-disk media storage for product photos and long-form videos.
    /// Files are written under wwwroot/media/{yyyy}/{MM} and served statically.
    /// No third-party hosting (Cloudinary) is used.
    /// </summary>
    public static class MediaStore
    {
        public const long MaxImageBytes = 50L * 1024 * 1024;
        public const long MaxVideoBytes = 600L * 1024 * 1024;

        private static readonly HashSet<string> AllowedImages = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif", ".bmp"
        };

        private static readonly HashSet<string> AllowedVideos = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".webm", ".mov", ".m4v"
        };

        public static bool IsImage(string extension) => AllowedImages.Contains(extension);
        public static bool IsVideo(string extension) => AllowedVideos.Contains(extension);

        /// <summary>
        /// Saves an uploaded file to the local media folder and returns its public
        /// relative URL (e.g. /media/2026/09/{guid}.jpg). Returns null when invalid.
        /// </summary>
        public static string? Save(IFormFile file, IWebHostEnvironment env)
        {
            if (file == null || file.Length <= 0) return null;

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || (!IsImage(ext) && !IsVideo(ext))) return null;

            var max = IsVideo(ext) ? MaxVideoBytes : MaxImageBytes;
            if (!file.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase)
                && !file.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (file.Length > max) return null;

            var now = DateTime.Now;
            var relDir = Path.Combine("media", now.ToString("yyyy"), now.ToString("MM"));
            var absDir = Path.Combine(env.WebRootPath, relDir);
            Directory.CreateDirectory(absDir);

            var name = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var relPath = relDir.Replace('\\', '/') + "/" + name;
            var absPath = Path.Combine(absDir, name);

            try
            {
                using var stream = new FileStream(absPath, FileMode.Create, FileAccess.Write);
                file.CopyTo(stream);
                return "/" + relPath;
            }
            catch
            {
                try { if (File.Exists(absPath)) File.Delete(absPath); } catch { }
                return null;
            }
        }

        /// <summary>Deletes a local media file given its public URL path.</summary>
        public static bool Delete(string? url, IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var clean = url.Split('?', '#')[0];
            if (!clean.StartsWith("/media/", StringComparison.OrdinalIgnoreCase)) return false;

            var name = clean.Substring(1).Replace('/', Path.DirectorySeparatorChar);
            var absPath = Path.Combine(env.WebRootPath, name);
            try
            {
                if (File.Exists(absPath)) { File.Delete(absPath); return true; }
            }
            catch { }
            return false;
        }

        /// <summary>Lists stored media newest-first as {url, size, name}.</summary>
        public static List<MediaItem> List(IWebHostEnvironment env, int take = 200)
        {
            var root = Path.Combine(env.WebRootPath, "media");
            var items = new List<MediaItem>();
            if (!Directory.Exists(root)) return items;

            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var fi = new FileInfo(file);
                var rel = Path.GetRelativePath(env.WebRootPath, file).Replace('\\', '/');
                items.Add(new MediaItem
                {
                    Url = "/" + rel,
                    Name = fi.Name,
                    Size = fi.Length,
                    Modified = fi.LastWriteTime
                });
            }

            return items.OrderByDescending(i => i.Modified).Take(take).ToList();
        }
    }

    public class MediaItem
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime Modified { get; set; }
        public bool IsVideo => Url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
            || Url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
            || Url.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
            || Url.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase);
    }
}
using System;

namespace Echo.Models
{
    public class UpdateInfo
    {
        public Version Version { get; set; } = new Version(1, 0, 0);
        public string DownloadUrl { get; set; } = "";
        public string Changelog { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public long FileSize { get; set; }
        public string FileName { get; set; } = "";
        
        public string FormattedSize => FormatBytes(FileSize);
        public string VersionString => $"v{Version.Major}.{Version.Minor}.{Version.Build}";

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}

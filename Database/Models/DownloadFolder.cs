using System;

namespace YoutubeDownloader.Database.Models
{
    public class DownloadFolder
    {
        public int DownloadFolderId { get; set; }
        public string Path { get; set; }
        public DateTime LastSelectionDate { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}
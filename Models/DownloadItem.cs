using System.Collections.Generic;

namespace YoutubeDownloader.Models
{
    public class DownloadItem
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public List<DownloadItem> Entries { get; set; }

        public override string ToString()
        {
            return $"FileName={Name} Link={Link}";
        }
    }
}
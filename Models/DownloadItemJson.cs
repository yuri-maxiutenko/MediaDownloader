namespace YoutubeDownloader.Models
{
    public class DownloadItemJson
    {
        public string id { get; set; }
        public string ext { get; set; }
        public string title { get; set; }
        public string webpage_url { get; set; }
        public DownloadItemJson[] entries { get; set; }
        public DownloadItemFormatJson[] requested_formats { get; set; }
    }

    public class DownloadItemFormatJson
    {
        public string format { get; set; }
        public string ext { get; set; }
        public string vcodec { get; set; }
        public string acodec { get; set; }
    }
}
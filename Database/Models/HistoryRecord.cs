using System;

namespace YoutubeDownloader.Database.Models
{
    public class HistoryRecord
    {
        public int HistoryRecordId { get; set; }
        public string FileName { get; set; }
        public string Url { get; set; }
        public DateTime DownloadDate { get; set; }
        public int DownloadStatus { get; set; }
        public int DownloadFormat { get; set; }
    }
}
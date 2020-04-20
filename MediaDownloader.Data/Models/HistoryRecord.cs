using System;
using System.ComponentModel.DataAnnotations;

namespace MediaDownloader.Data.Models
{
    public class HistoryRecord
    {
        [Key]
        public int HistoryRecordId { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public DateTime DownloadDate { get; set; }
        public int DownloadStatus { get; set; }
        public int DownloadFormat { get; set; }
    }
}
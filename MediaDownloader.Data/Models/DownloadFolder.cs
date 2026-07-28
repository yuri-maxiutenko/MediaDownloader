using System;
using System.ComponentModel.DataAnnotations;

namespace MediaDownloader.Data.Models;

public class DownloadFolder
{
    [Key]
    public int DownloadFolderId { get; init; }

    public required string Path { get; set; }
    public DateTime LastSelectionDate { get; set; }

    public override string ToString()
    {
        return Path;
    }
}
namespace MediaDownloader.Models;

public class ProgressReportModel
{
    public required string Message { get; init; }
    public double? Value { get; set; }
    public string? FilePath { get; set; }
}
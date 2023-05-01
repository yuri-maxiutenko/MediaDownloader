namespace MediaDownloader.Models;

public class ProgressReportModel
{
    public string Message { get; set; }
    public double? Value { get; set; }
    public string FilePath { get; set; }
}
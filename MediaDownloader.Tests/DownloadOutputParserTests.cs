using MediaDownloader.Download.Models;
using MediaDownloader.Download.Utilities;

namespace MediaDownloader.Tests;

public class DownloadOutputParserTests
{
    [Theory]
    [InlineData("[download]  42.7% of 10.00MiB at 5.00MiB/s ETA 00:01", 42.7)]
    [InlineData("[download] 100% of 10.00MiB in 00:00:05 at 2.00MiB/s", 100)]
    [InlineData("[download]   0.0% of ~123.45MiB at Unknown speed ETA Unknown", 0)]
    public void TryParseDownloadProgress_ProgressLine_ReturnsPercent(string line, double expected)
    {
        Assert.True(DownloadOutputParser.TryParseDownloadProgress(line, out var percent));
        Assert.Equal(expected, percent, 3);
    }

    [Theory]
    [InlineData("[info] Writing video metadata as JSON")]
    [InlineData("[download] Destination: video.mp4")]
    [InlineData("")]
    public void TryParseDownloadProgress_NonProgressLine_ReturnsFalse(string line)
    {
        Assert.False(DownloadOutputParser.TryParseDownloadProgress(line, out _));
    }

    [Theory]
    [InlineData("[ffmpeg] Merging formats into \"C:\\Videos\\My Clip.mp4\"")]
    [InlineData("[Merger] Merging formats into \"C:\\Videos\\My Clip.mp4\"")]
    public void TryParseResultFilePath_MergeLine_ReturnsPath(string line)
    {
        Assert.True(DownloadOutputParser.TryParseResultFilePath(line, out var path));
        Assert.Equal("C:\\Videos\\My Clip.mp4", path);
    }

    [Theory]
    [InlineData("[download] C:\\Videos\\My Clip.mp4 has already been downloaded and merged")]
    [InlineData("[download] C:\\Videos\\My Clip.mp4 has already been downloaded")]
    public void TryParseResultFilePath_AlreadyDownloadedLine_ReturnsPath(string line)
    {
        Assert.True(DownloadOutputParser.TryParseResultFilePath(line, out var path));
        Assert.Equal("C:\\Videos\\My Clip.mp4", path);
    }

    [Theory]
    [InlineData("[download] Downloading playlist: stuff")]
    [InlineData("")]
    public void TryParseResultFilePath_NonMatchingLine_ReturnsFalse(string line)
    {
        Assert.False(DownloadOutputParser.TryParseResultFilePath(line, out _));
    }

    [Theory]
    [InlineData("WARNING: [youtube] No supported JavaScript runtime could be found.")]
    [InlineData("WARNING: Falling back to generic extractor")]
    [InlineData("  warning: lowercase is still a warning")]
    public void GetSeverity_WarningLines_ReturnWarning(string line)
    {
        Assert.Equal(DownloadLogSeverity.Warning, DownloadOutputParser.GetSeverity(line));
    }

    [Theory]
    [InlineData("ERROR: unable to download video data: HTTP Error 403")]
    [InlineData("[youtube] ERROR: Video unavailable")]
    public void GetSeverity_ErrorLines_ReturnError(string line)
    {
        Assert.Equal(DownloadLogSeverity.Error, DownloadOutputParser.GetSeverity(line));
    }

    [Theory]
    [InlineData("[download]  42.7% of 10.00MiB at 5.00MiB/s ETA 00:01")]
    [InlineData("[youtube] jNQXAC9IVRw: Downloading webpage")]
    [InlineData("")]
    // A mention of the word later in the line is not a severity marker.
    [InlineData("[download] Destination: WARNING misleading name.mp4")]
    public void GetSeverity_OrdinaryLines_ReturnInfo(string line)
    {
        Assert.Equal(DownloadLogSeverity.Info, DownloadOutputParser.GetSeverity(line));
    }
}

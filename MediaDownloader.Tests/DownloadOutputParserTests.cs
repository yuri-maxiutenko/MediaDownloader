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

    [Fact]
    public void TryParseResultFilePath_MergeLine_ReturnsPath()
    {
        var line = "[ffmpeg] Merging formats into \"C:\\Videos\\My Clip.mp4\"";

        Assert.True(DownloadOutputParser.TryParseResultFilePath(line, out var path));
        Assert.Equal("C:\\Videos\\My Clip.mp4", path);
    }

    [Fact]
    public void TryParseResultFilePath_AlreadyDownloadedLine_ReturnsPath()
    {
        var line = "[download] C:\\Videos\\My Clip.mp4 has already been downloaded and merged";

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
}

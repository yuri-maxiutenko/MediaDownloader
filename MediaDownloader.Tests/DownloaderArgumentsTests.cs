using MediaDownloader.Download;
using MediaDownloader.Download.Utilities;

namespace MediaDownloader.Tests;

public class DownloaderArgumentsTests
{
    [Fact]
    public void BuildGetItemsArguments_KeepsLinkAsSingleArgumentAfterSeparator()
    {
        var link = "https://example.com/\" --exec calc \"";

        var arguments = Downloader.BuildGetItemsArguments(link, "bestvideo+bestaudio/best");

        Assert.Equal(link, arguments[^1]);
        Assert.Equal("--", arguments[^2]);
        Assert.DoesNotContain(arguments, argument => argument.Contains("--exec") && argument != link);
    }

    [Fact]
    public void BuildDownloadArguments_KeepsPathsAndLinkAsSingleArguments()
    {
        var path = @"C:\Down loads\video ""quoted"".mp4";
        var link = "https://example.com/watch?v=1&x=2";
        var converter = @"C:\Program Files\ffmpeg\bin\ffmpeg.exe";

        var arguments = Downloader.BuildDownloadArguments(path, link, "best", converter);

        Assert.Equal(link, arguments[^1]);
        Assert.Equal("--", arguments[^2]);
        Assert.Equal(path, arguments[arguments.IndexOf("-o") + 1]);
        Assert.Equal(converter, arguments[arguments.IndexOf("--ffmpeg-location") + 1]);
    }

    [Fact]
    public void BuildUpdateArguments_IsJustTheUpdateFlag()
    {
        Assert.Equal(["-U"], Downloader.BuildUpdateArguments());
    }
}

public class UrlValidatorTests
{
    [Theory]
    [InlineData("https://example.com/watch?v=1")]
    [InlineData("http://example.com")]
    public void IsValidHttpUrl_HttpAndHttps_ReturnsTrue(string url)
    {
        Assert.True(UrlValidator.IsValidHttpUrl(url));
    }

    [Theory]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("--exec calc")]
    [InlineData("not a url")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidHttpUrl_NonHttpSchemesAndGarbage_ReturnsFalse(string? url)
    {
        Assert.False(UrlValidator.IsValidHttpUrl(url));
    }
}

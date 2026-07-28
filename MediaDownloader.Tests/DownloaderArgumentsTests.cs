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

    [Fact]
    public void BuildGetItemsArguments_EnablesNodeAndBundledQuickJs()
    {
        var jsRuntime = @"C:\Program Files\Media Downloader\quickjs\qjs.exe";

        var arguments = Downloader.BuildGetItemsArguments("https://example.com/v", "best", jsRuntime);

        // "node" is enabled so a user's own installation is preferred over the bundled runtime.
        Assert.Contains("node", arguments);
        // The runtime spec must survive as a single argv element, spaces and all.
        Assert.Contains($"quickjs:{jsRuntime}", arguments);
        Assert.Equal("--js-runtimes", arguments[arguments.IndexOf($"quickjs:{jsRuntime}") - 1]);
        Assert.Equal("--js-runtimes", arguments[arguments.IndexOf("node") - 1]);
    }

    [Fact]
    public void BuildDownloadArguments_EnablesBundledQuickJs()
    {
        var jsRuntime = @"C:\tools\quickjs\qjs.exe";

        var arguments = Downloader.BuildDownloadArguments(@"C:\out\v.mp4", "https://example.com/v", "best",
            @"C:\ffmpeg.exe", jsRuntime);

        Assert.Contains($"quickjs:{jsRuntime}", arguments);
        // Runtime flags must not disturb the trailing "-- <url>" separator.
        Assert.Equal("https://example.com/v", arguments[^1]);
        Assert.Equal("--", arguments[^2]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildGetItemsArguments_WithoutJsRuntimePath_OmitsQuickJs(string? jsRuntime)
    {
        var arguments = Downloader.BuildGetItemsArguments("https://example.com/v", "best", jsRuntime);

        Assert.DoesNotContain(arguments, argument => argument.StartsWith("quickjs:", StringComparison.Ordinal));
        // node stays enabled: it costs nothing and may be present on the machine.
        Assert.Contains("node", arguments);
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

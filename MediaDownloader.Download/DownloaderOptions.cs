namespace MediaDownloader.Download;

public sealed class DownloaderOptions
{
    public string DownloaderPath { get; set; } = string.Empty;
    public string ConverterPath { get; set; } = string.Empty;

    /// <summary>
    ///     QuickJS binary used to solve YouTube's JavaScript challenges. Optional: without it
    ///     yt-dlp still works, but YouTube extraction is degraded and some formats are missing.
    /// </summary>
    public string JsRuntimePath { get; set; } = string.Empty;
}

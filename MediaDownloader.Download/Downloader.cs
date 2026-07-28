using System.Diagnostics;
using System.Globalization;
using System.Text;

using MediaDownloader.Download.Models;
using MediaDownloader.Download.Properties;
using MediaDownloader.Download.Utilities;

namespace MediaDownloader.Download;

public class Downloader : IDownloader
{
    private const int DownloadTimeoutSec = 60;

    private readonly string _converterPath;

    private readonly Dictionary<DownloadFormatType, string> _downloadFormats = new()
    {
        {
            DownloadFormatType.Best, Resources.DownloaderOptionFormatBest
        },
        {
            DownloadFormatType.BestMp4, Resources.DownloaderOptionFormatBestMp4
        },
        {
            DownloadFormatType.BestDirectLink, Resources.DownloaderOptionFormatBestDirectLink
        },
        {
            DownloadFormatType.AudioOnly, Resources.DownloaderOptionFormatAudioOnly
        }
    };

    private readonly string _downloaderPath;

    private readonly string? _jsRuntimePath;

    public Downloader(string downloaderPath, string converterPath, string? jsRuntimePath = null)
    {
        // Relative tool paths must resolve against the install directory, never the
        // process working directory, which is attacker-influenced (e.g. "Open with").
        _downloaderPath = Path.GetFullPath(downloaderPath, AppContext.BaseDirectory);
        _converterPath = Path.GetFullPath(converterPath, AppContext.BaseDirectory);
        _jsRuntimePath = string.IsNullOrWhiteSpace(jsRuntimePath)
            ? null
            : Path.GetFullPath(jsRuntimePath, AppContext.BaseDirectory);
    }

    public async Task<DownloadItem?> GetItemsAsync(
        string? link,
        DownloadFormatType downloadFormatType,
        DataReceivedEventHandler onErrorDataReceived,
        CancellationToken cancellationToken)
    {
        if (!UrlValidator.IsValidHttpUrl(link))
        {
            return null;
        }

        var downloaderProcess = CreateDownloaderProcess(
            BuildGetItemsArguments(link!, _downloadFormats[downloadFormatType], _jsRuntimePath));

        var outputReader = new StringBuilder();

        void OnOutputDataReceived(object _, DataReceivedEventArgs args)
        {
            outputReader.Append(args.Data);
        }

        try
        {
            downloaderProcess.ErrorDataReceived += onErrorDataReceived;
            downloaderProcess.OutputDataReceived += OnOutputDataReceived;

            downloaderProcess.Start();

            downloaderProcess.BeginErrorReadLine();
            downloaderProcess.BeginOutputReadLine();

            await downloaderProcess.WaitForExitAsync(cancellationToken);

            if (downloaderProcess.ExitCode != 0)
            {
                return null;
            }

            return DownloadItemMapper.Map(outputReader.ToString(), link);
        }
        catch (OperationCanceledException)
        {
            if (!downloaderProcess.HasExited)
            {
                downloaderProcess.Kill();
            }

            throw;
        }
        finally
        {
            downloaderProcess.ErrorDataReceived -= onErrorDataReceived;
            downloaderProcess.OutputDataReceived -= OnOutputDataReceived;
        }
    }

    public async Task<bool> DownloadItemAsync(
        string downloadFilePath,
        string link,
        DownloadFormatType downloadFormatType,
        DataReceivedEventHandler onOutputReceived,
        DataReceivedEventHandler onErrorReceived,
        CancellationToken cancellationToken)
    {
        if (!UrlValidator.IsValidHttpUrl(link))
        {
            return false;
        }

        var downloaderProcess = CreateDownloaderProcess(
            BuildDownloadArguments(downloadFilePath, link, _downloadFormats[downloadFormatType], _converterPath,
                _jsRuntimePath));

        return await ExecuteDownloaderAsync(downloaderProcess, onOutputReceived, onErrorReceived, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(
        DataReceivedEventHandler onOutputReceived,
        DataReceivedEventHandler onErrorReceived,
        CancellationToken cancellationToken)
    {
        var downloaderProcess = CreateDownloaderProcess(BuildUpdateArguments());

        return await ExecuteDownloaderAsync(downloaderProcess, onOutputReceived, onErrorReceived, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static List<string> BuildGetItemsArguments(string link, string format, string? jsRuntimePath = null) =>
    [
        .. BuildCommonArguments(jsRuntimePath),
        "-f", format,
        "-J",
        "--",
        link
    ];

    internal static List<string> BuildDownloadArguments(
        string downloadFilePath,
        string link,
        string format,
        string converterPath,
        string? jsRuntimePath = null) =>
    [
        .. BuildCommonArguments(jsRuntimePath),
        "--no-mtime",
        "--no-playlist",
        "-f", format,
        "-o", downloadFilePath,
        "--ffmpeg-location", converterPath,
        "--",
        link
    ];

    // Self-updating needs no JavaScript runtime.
    internal static List<string> BuildUpdateArguments() => ["-U"];

    private static List<string> BuildCommonArguments(string? jsRuntimePath)
    {
        List<string> arguments =
        [
            "--encoding", "utf-8",
            "--socket-timeout", DownloadTimeoutSec.ToString(CultureInfo.InvariantCulture)
        ];

        // YouTube extraction needs a JavaScript runtime, otherwise formats are missing.
        // Only "deno" is enabled by default, so additionally enable "node" and the bundled
        // QuickJS. yt-dlp picks the highest priority available runtime (deno > node >
        // quickjs), so a user's own deno/node installation wins over the bundled fallback.
        arguments.AddRange(["--js-runtimes", "node"]);

        if (!string.IsNullOrWhiteSpace(jsRuntimePath))
        {
            arguments.AddRange(["--js-runtimes", $"quickjs:{jsRuntimePath}"]);
        }

        return arguments;
    }

    private Process CreateDownloaderProcess(IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _downloaderPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return new Process
        {
            StartInfo = startInfo
        };
    }

    private static async Task<bool> ExecuteDownloaderAsync(
        Process downloaderProcess,
        DataReceivedEventHandler onOutputReceived,
        DataReceivedEventHandler onErrorReceived,
        CancellationToken cancellationToken)
    {
        try
        {
            downloaderProcess.OutputDataReceived += onOutputReceived;
            downloaderProcess.ErrorDataReceived += onErrorReceived;

            downloaderProcess.Start();

            downloaderProcess.BeginErrorReadLine();
            downloaderProcess.BeginOutputReadLine();

            await downloaderProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            return downloaderProcess.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            if (!downloaderProcess.HasExited)
            {
                downloaderProcess.Kill();
            }

            throw;
        }
        finally
        {
            downloaderProcess.OutputDataReceived -= onOutputReceived;
            downloaderProcess.ErrorDataReceived -= onErrorReceived;
        }
    }
}
